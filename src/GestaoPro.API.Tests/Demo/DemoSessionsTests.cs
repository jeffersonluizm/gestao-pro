using System.IdentityModel.Tokens.Jwt;
using GestaoPro.API.Services.Demo;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Tests.Demo;

// POST /api/demo/entrar: a API devolve a sessão da conta demo do papel pedido, sem senha nenhuma em jogo.
public class DemoSessionsTests
{
    private const string SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";

    private readonly Mock<IAuthAccounts> _accounts = new();
    private readonly DemoCatalog _catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build());

    private static IOptions<JwtSettings> Jwt() => Options.Create(new JwtSettings
    {
        SecretKey = SecretKey, Issuer = "GestaoPro.API", Audience = "GestaoPro.API", ExpirationHours = 8,
    });

    private DemoSessions Service() => new(_catalog, _accounts.Object, new TokenService(Jwt(), _catalog));

    private void ContaPronta(bool ativa = true) =>
        _accounts.Setup(a => a.GetCredentialByRegistrationNumberAsync("morador@demo.gestaopro.local")).ReturnsAsync(new Credential
        {
            UserId = "665f1c2e9b1e8a00000000d2", RegistrationNumber = "morador@demo.gestaopro.local", Role = "morador",
            CondominiumId = "665f1c2e9b1e8a00000000c1", IsActive = ativa, MustChangePassword = false,
            PasswordHash = DemoCatalog.UnusablePasswordHash(),
        });

    [Fact]
    public async Task Devolve_token_da_conta_com_a_epoca_atual_da_demonstracao()
    {
        ContaPronta();
        _accounts.Setup(a => a.GetUserByIdAsync("665f1c2e9b1e8a00000000d2")).ReturnsAsync(new User { Name = "Maria Souza" });

        var session = await Service().EnterAsync("morador");

        Assert.NotNull(session);
        Assert.Equal("morador", session!.Role);
        Assert.Equal("Maria Souza", session.UserName);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.Token);
        Assert.Equal(_catalog.Epoch, jwt.Claims.Single(c => c.Type == DemoCatalog.EpochClaim).Value);
    }

    [Fact]
    public async Task Conta_inexistente_ou_inativa_nao_gera_sessao()
    {
        Assert.Null(await Service().EnterAsync("morador"));

        ContaPronta(ativa: false);
        Assert.Null(await Service().EnterAsync("morador"));
    }

    // Login por senha numa conta demo falha: o hash guardado é de um valor aleatório.
    [Fact]
    public async Task Login_por_senha_numa_conta_demo_e_recusado()
    {
        ContaPronta();
        var auth = new AuthAppService(_accounts.Object, new BcryptPasswordVerifier(), new TokenService(Jwt()));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            auth.LoginAsync(new LoginRequest("morador@demo.gestaopro.local", "Demo-2026-qualquer")));

        Assert.Equal(AuthAppService.InvalidLoginMessage, error.Message);
    }

    // Sem a flag, o token não leva época (instalações reais não são afetadas).
    [Fact]
    public void Sem_a_flag_o_token_nao_leva_a_epoca()
    {
        var (token, _) = new TokenService(Jwt(), DemoCatalog.Disabled).GenerateToken(new Credential
        {
            UserId = "u1", Role = "morador", CondominiumId = "c1", RegistrationNumber = "maria@teste.com",
        });

        Assert.DoesNotContain(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims, c => c.Type == DemoCatalog.EpochClaim);
    }
}
