using System.Reflection;
using GestaoPro.API.Controllers;
using GestaoPro.API.Middleware;
using GestaoPro.API.Services.Demo;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Demo;

// Re-revisão de segurança do modo demonstração: credenciais pela rota da administração, sessões emitidas pela
// entrada sem senha e a janela do reset.
public class DemoCredentialRouteTests
{
    private const string Seeded = "665f1c2e9b1e8a00000000e1";
    private const string Visitor = "665f1c2e9b1e8a00000000e2";
    private readonly Mock<IUserDirectory> _directory = new();
    private readonly DemoCatalog _catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build());

    public DemoCredentialRouteTests()
    {
        _catalog.CondominiumId = CondominioA;
        _catalog.SetSeededRecords(new[] { Seeded });
        _directory.Setup(d => d.GetByIdAsync(Seeded)).ReturnsAsync(new UserResponse { Id = Seeded, CondominiumId = CondominioA, Email = "11111111111@pending.gestaopro.local", Role = UserRole.morador });
        _directory.Setup(d => d.GetByIdAsync(Visitor)).ReturnsAsync(new UserResponse { Id = Visitor, CondominiumId = CondominioA, Email = "ana@exemplo.test", Role = UserRole.morador });
    }

    // O serviço não é construído: as recusas vêm antes dele.
    private AuthController Auth(DemoCatalog? catalog = null) =>
        new AuthController(null!, _directory.Object, null!, new CondominiumAttemptLimiter(100, TimeSpan.FromMinutes(1)), catalog ?? _catalog)
            .As("665f1c2e9b1e8a00000000d1", "sindico");

    private static CreateCredentialRequest Request(string userId, string login = "nova@exemplo.test", string role = "morador") =>
        new(userId, login, "Senha-Forte-Da-Ana-2026", role, CondominioA);

    [Fact]
    public async Task Credencial_para_registro_semeado_e_recusada()
    {
        Assert.Equal(403, StatusOf(await Auth().CreateCredential(Request(Seeded, role: "admin"))));
    }

    [Fact]
    public async Task Login_no_dominio_reservado_da_demo_e_recusado()
    {
        Assert.Equal(400, StatusOf(await Auth().CreateCredential(Request(Visitor, login: "sindico2@demo.gestaopro.local"))));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sindico")]
    [InlineData("superusuario")]
    public async Task Papel_da_credencial_precisa_ser_o_do_cadastro_da_pessoa(string role)
    {
        var result = await Auth(DemoCatalog.Disabled).CreateCredential(Request(Visitor, role: role));

        Assert.Equal(400, StatusOf(result));
    }

    [Fact]
    public async Task Condominio_da_credencial_precisa_ser_o_da_pessoa()
    {
        var request = new CreateCredentialRequest(Visitor, "nova@exemplo.test", "Senha-Forte-Da-Ana-2026", "morador", CondominioB);

        Assert.NotEqual(201, StatusOf(await Auth(DemoCatalog.Disabled).CreateCredential(request)));
    }

    [Fact]
    public async Task Redefinir_senha_de_registro_semeado_e_recusado()
    {
        Assert.Equal(403, StatusOf(await Auth().ResetPassword(new AuthResetPasswordRequest(Seeded, "Senha-Forte-Da-Ana-2026"))));
    }
}

public class DemoSessionChecksTests
{
    private const string DemoCondominium = "665f1c2e9b1e8a00000000c1";
    private readonly Mock<IAuthAccounts> _accounts = new();
    private readonly DemoCatalog _catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build());

    public DemoSessionChecksTests() => _catalog.CondominiumId = DemoCondominium;

    private DemoSessions Service() => new(_catalog, _accounts.Object, new TokenService(Options.Create(new JwtSettings
    {
        SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789", Issuer = "GestaoPro.API", Audience = "GestaoPro.API",
    }), _catalog));

    private void Credential(string condominiumId, string role, string userEmail) =>
        Setup(condominiumId, role, userEmail, "665f1c2e9b1e8a00000000d1");

    private void Setup(string condominiumId, string role, string userEmail, string userId)
    {
        _accounts.Setup(a => a.GetCredentialByRegistrationNumberAsync("sindico@demo.gestaopro.local")).ReturnsAsync(new Credential
        {
            UserId = userId, RegistrationNumber = "sindico@demo.gestaopro.local", Role = role, CondominiumId = condominiumId, IsActive = true,
        });
        _accounts.Setup(a => a.GetUserByIdAsync(userId)).ReturnsAsync(new User { Id = userId, Email = userEmail, CondominiumId = condominiumId, Name = "Ana" });
    }

    [Fact]
    public async Task Credencial_certa_gera_sessao()
    {
        Credential(DemoCondominium, "sindico", "sindico@demo.gestaopro.local");

        Assert.NotNull(await Service().EnterAsync("sindico"));
    }

    [Theory]
    [InlineData("665f1c2e9b1e8a00000000ff", "sindico", "sindico@demo.gestaopro.local")]
    [InlineData(DemoCondominium, "admin", "sindico@demo.gestaopro.local")]
    [InlineData(DemoCondominium, "sindico", "intruso@exemplo.test")]
    public async Task Credencial_fora_do_esperado_nao_gera_sessao(string condominiumId, string role, string email)
    {
        Credential(condominiumId, role, email);

        Assert.Null(await Service().EnterAsync("sindico"));
    }
}

public class DemoResetWindowTests
{
    private readonly DemoCatalog _catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build());
    private readonly DemoResetGate _gate = new();

    private sealed class FakeCleaner(Action onClean) : IDemoCleaner
    {
        public int Calls { get; private set; }
        public Task ResetAsync(CancellationToken cancellationToken = default) { Calls++; onClean(); return Task.CompletedTask; }
    }

    private sealed class FakeSeeder(int failures) : IDemoSeeder
    {
        public int Calls { get; private set; }
        public Task SeedAsync()
        {
            Calls++;
            return Calls <= failures ? throw new InvalidOperationException("chave duplicada") : Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Troca_a_epoca_antes_de_limpar_e_fecha_as_escritas_durante_o_reset()
    {
        var before = _catalog.Epoch;
        string? epochWhileCleaning = null;
        var resettingWhileCleaning = false;
        var cleaner = new FakeCleaner(() => { epochWhileCleaning = _catalog.Epoch; resettingWhileCleaning = _gate.Resetting; });

        await new DemoResetRunner(cleaner, new FakeSeeder(0), _catalog, _gate).ResetAsync();

        Assert.NotEqual(before, epochWhileCleaning);
        Assert.True(resettingWhileCleaning);
        Assert.False(_gate.Resetting);
    }

    // Um conflito ao semear (dado criado na janela) não aborta o reset: limpa e semeia de novo.
    [Fact]
    public async Task Conflito_ao_semear_limpa_e_semeia_de_novo()
    {
        var cleaner = new FakeCleaner(() => { });
        var seeder = new FakeSeeder(failures: 1);

        await new DemoResetRunner(cleaner, seeder, _catalog, _gate).ResetAsync();

        Assert.Equal(2, cleaner.Calls);
        Assert.Equal(2, seeder.Calls);
        Assert.False(_gate.Resetting);
    }

    [Fact]
    public async Task Escrita_em_andamento_termina_antes_de_a_limpeza_comecar()
    {
        Assert.True(_gate.TryEnterWrite());
        var cleaned = false;
        var reset = new DemoResetRunner(new FakeCleaner(() => cleaned = true), new FakeSeeder(0), _catalog, _gate).ResetAsync();

        await Task.Delay(100);
        Assert.False(cleaned);
        Assert.False(_gate.TryEnterWrite());

        _gate.ExitWrite();
        await reset;
        Assert.True(cleaned);
    }

    [Theory]
    [InlineData("POST", "/api/announcements", 503)]
    [InlineData("POST", "/api/demo/entrar", 503)]
    [InlineData("POST", "/api/auth/login", 503)]
    [InlineData("GET", "/api/announcements", 200)]
    public async Task Durante_o_reset_escritas_respondem_503_com_Retry_After(string method, string path, int expected)
    {
        await _gate.BeginAsync(TimeSpan.FromSeconds(1));
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        var middleware = new DemoResetGateMiddleware(_ => Task.CompletedTask, _gate, _catalog);

        await middleware.InvokeAsync(context);

        Assert.Equal(expected, context.Response.StatusCode);
        if (expected == 503) Assert.False(string.IsNullOrEmpty(context.Response.Headers.RetryAfter));
    }
}

public class DemoCondominiumRestoreTests
{
    // Regras, regulamento e endereço do condomínio demo voltam ao original no reset.
    [Fact]
    public void Restaura_nome_regras_regulamento_e_endereco()
    {
        var condominium = new Condominium
        {
            Name = "Outro nome", Rules = "Texto gravado por um visitante", Regulations = "Outro regulamento",
            Address = new Address { Street = "Rua trocada", City = "Outra cidade" },
        };

        DemoCondominium.Restore(condominium, "hash-do-codigo");

        Assert.Equal(DemoCatalog.CondominiumName, condominium.Name);
        Assert.Equal(DemoCondominium.Rules, condominium.Rules);
        Assert.Equal(DemoCondominium.Regulations, condominium.Regulations);
        Assert.Equal("Rua das Acácias", condominium.Address.Street);
        Assert.Equal("Belo Horizonte", condominium.Address.City);
        Assert.Equal("hash-do-codigo", condominium.DefaultPassword);
    }
}

