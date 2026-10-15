using System.Text.Json;
using GestaoPro.API.Controllers;
using GestaoPro.API.Services.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GestaoPro.API.Tests.Demo;

// Modo demonstração: uma flag de servidor (Demo:Enabled, desligada por padrão), sem segredo no código.
// As contas demo não têm senha publicada: a entrada é por POST /api/demo/entrar. O código do condomínio vem da
// configuração ou é sorteado na partida (ele continua visível: serve para demonstrar a ativação por CPF).
public class DemoCatalogTests
{
    private static DemoCatalog Catalog(params (string Key, string? Value)[] settings) =>
        DemoCatalog.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => s.Value))
            .Build());

    [Fact]
    public void Sem_a_flag_o_modo_demo_fica_desligado_e_sem_contas()
    {
        var catalog = Catalog();

        Assert.False(catalog.Enabled);
        Assert.Empty(catalog.Accounts);
    }

    [Fact]
    public void Com_a_flag_e_configuracao_usa_o_codigo_configurado()
    {
        var catalog = Catalog(("Demo:Enabled", "true"), ("Demo:CodigoCondominio", "Codigo-Da-Demo-2026"));

        Assert.True(catalog.Enabled);
        Assert.Equal("Codigo-Da-Demo-2026", catalog.CondominiumCode);
        Assert.Equal(new[] { "sindico", "morador", "porteiro" }, catalog.Accounts.Select(a => a.Role));
    }

    [Fact]
    public void Com_a_flag_e_sem_configuracao_sorteia_um_codigo_forte()
    {
        var first = Catalog(("Demo:Enabled", "true"));
        var second = Catalog(("Demo:Enabled", "true"));

        Assert.True(CondominiumCodePolicy.IsStrong(first.CondominiumCode));
        Assert.NotEqual(first.CondominiumCode, second.CondominiumCode);
    }

    [Fact]
    public void Codigo_fraco_na_configuracao_impede_a_partida()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Catalog(("Demo:Enabled", "true"), ("Demo:CodigoCondominio", "123456")));

        Assert.Contains("Demo__CodigoCondominio", error.Message);
    }

    // A senha das contas demo é um hash de valor aleatório que ninguém conhece: login por senha nelas falha.
    [Fact]
    public void Hash_das_contas_demo_nao_aceita_nenhuma_senha_conhecida()
    {
        var hash = DemoCatalog.UnusablePasswordHash();

        Assert.StartsWith("$2", hash);
        Assert.NotEqual(hash, DemoCatalog.UnusablePasswordHash());
        foreach (var guess in new[] { "", "Admin@123", "Demo-2026", "sindico@demo.gestaopro.local" })
            Assert.False(BCrypt.Net.BCrypt.Verify(guess, hash));
    }

    // Época da demonstração: muda a cada reset. Token de uma época anterior deixa de valer.
    [Fact]
    public void Cada_reset_troca_a_epoca_da_demonstracao()
    {
        var catalog = Catalog(("Demo:Enabled", "true"));
        var before = catalog.Epoch;

        catalog.RenewEpoch();

        Assert.False(string.IsNullOrWhiteSpace(before));
        Assert.NotEqual(before, catalog.Epoch);
    }

    [Fact]
    public void Contas_demo_sao_reconhecidas_pelo_email_so_com_a_flag_ligada()
    {
        var on = Catalog(("Demo:Enabled", "true"));
        var off = Catalog();

        Assert.True(on.IsDemoEmail("MORADOR@demo.gestaopro.local"));
        Assert.False(on.IsDemoEmail("maria@condominio.com"));
        Assert.False(off.IsDemoEmail("morador@demo.gestaopro.local"));
        // Protegidas são só as contas publicadas; outro e-mail do domínio demo não ganha proteção.
        Assert.False(on.IsDemoEmail("intruso@demo.gestaopro.local"));
        Assert.True(on.IsReservedDomain("intruso@demo.gestaopro.local"));
        Assert.False(off.IsReservedDomain("intruso@demo.gestaopro.local"));
    }

    [Fact]
    public void Registros_semeados_ficam_marcados_ate_o_proximo_reset()
    {
        var catalog = Catalog(("Demo:Enabled", "true"));

        catalog.SetSeededRecords(new[] { "area-1", "unidade-101" });

        Assert.True(catalog.IsSeededRecord("area-1"));
        Assert.False(catalog.IsSeededRecord("area-criada-por-visitante"));
        Assert.False(Catalog().IsSeededRecord("area-1"));
    }
}

public class DemoControllerTests
{
    private readonly Mock<IDemoSessions> _sessions = new();

    private static DemoCatalog Catalog(bool enabled) => DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Demo:Enabled"] = enabled.ToString(),
            ["Demo:CodigoCondominio"] = "Codigo-Da-Demo-2026",
        })
        .Build());

    private static DemoController Controller(bool enabled) => new(Catalog(enabled));

    [Fact]
    public void Sem_a_flag_o_endpoint_responde_404()
    {
        Assert.IsType<NotFoundResult>(Controller(enabled: false).Get());
    }

    [Fact]
    public void Com_a_flag_devolve_papeis_nomes_e_o_codigo_sem_email_nem_senha()
    {
        var result = Controller(enabled: true).Get();

        var body = Assert.IsType<DemoInfoResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Codigo-Da-Demo-2026", body.CondominiumCode);
        Assert.Equal(new[] { "sindico", "morador", "porteiro" }, body.Accounts.Select(a => a.Role));
        Assert.Contains(body.Accounts, account => account.Role == "sindico" && account.Name == "Ana Ribeiro");
        var json = JsonSerializer.Serialize(body).ToLowerInvariant();
        Assert.DoesNotContain("password", json);
        Assert.DoesNotContain("senha", json);
        Assert.DoesNotContain("@demo.gestaopro.local", json);
        // Moradores pré-cadastrados (sem acesso) para experimentar a ativação com o código do condomínio.
        Assert.Equal(3, body.PreRegistered.Count);
        Assert.All(body.PreRegistered, resident => Assert.Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$", resident.Cpf));
    }

    [Fact]
    public async Task Entrar_sem_a_flag_responde_404()
    {
        var result = await Controller(enabled: false).Enter(new DemoEnterRequest("sindico"), _sessions.Object);

        Assert.IsType<NotFoundResult>(result);
        _sessions.Verify(s => s.EnterAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("")]
    [InlineData("sindico@demo.gestaopro.local")]
    public async Task Entrar_com_papel_fora_da_lista_responde_400(string papel)
    {
        var result = await Controller(enabled: true).Enter(new DemoEnterRequest(papel), _sessions.Object);

        Assert.Equal(400, Access.TestSession.StatusOf(result));
        _sessions.Verify(s => s.EnterAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Entrar_devolve_a_sessao_pronta_da_conta_do_papel()
    {
        var session = new LoginResponse("token", "u1", "Maria Souza", "morador@demo.gestaopro.local", "morador", "c1", false, DateTime.UtcNow.AddHours(8));
        _sessions.Setup(s => s.EnterAsync("morador")).ReturnsAsync(session);

        var result = await Controller(enabled: true).Enter(new DemoEnterRequest("Morador"), _sessions.Object);

        Assert.Same(session, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Conta_demo_ainda_nao_pronta_responde_503_com_mensagem()
    {
        _sessions.Setup(s => s.EnterAsync("porteiro")).ReturnsAsync((LoginResponse?)null);

        var result = await Controller(enabled: true).Enter(new DemoEnterRequest("porteiro"), _sessions.Object);

        Assert.Equal(503, Access.TestSession.StatusOf(result));
    }
}
