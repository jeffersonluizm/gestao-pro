using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Access;

// Login: toda tentativa roda exatamente uma verificação BCrypt, exista ou não a conta, esteja ou não ativa.
// Assim o tempo de resposta não separa "e-mail cadastrado" de "não cadastrado" (enumeração por tempo).
public class LoginTimingTests
{
    private const string SenhaCerta = "Senha-Correta-2026";

    private readonly Mock<IAuthAccounts> _accounts = new();
    private readonly Mock<IPasswordVerifier> _verifier = new();
    private readonly AuthAppService _service;

    private readonly Credential _ativa = new()
    {
        UserId = "joao", RegistrationNumber = "joao@teste.com", Role = "morador", CondominiumId = CondominioA,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(SenhaCerta, 4), MustChangePassword = false, IsActive = true,
    };

    public LoginTimingTests()
    {
        _verifier.Setup(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string text, string hash) => BCrypt.Net.BCrypt.Verify(text, hash));
        var token = new TokenService(Options.Create(new JwtSettings
        {
            SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789",
            Issuer = "GestaoPro.API", Audience = "GestaoPro.API", ExpirationHours = 1,
        }));
        _service = new AuthAppService(_accounts.Object, _verifier.Object, token);
    }

    private Task<LoginResponse> Login(string login, string password) =>
        _service.LoginAsync(new LoginRequest(login, password));

    [Fact]
    public async Task Conta_inexistente_roda_o_bcrypt_uma_vez_e_responde_igual()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Login("ninguem@teste.com", SenhaCerta));

        Assert.Equal(AuthAppService.InvalidLoginMessage, error.Message);
        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Credencial_desativada_roda_o_bcrypt_uma_vez_e_responde_igual()
    {
        _ativa.IsActive = false;
        _accounts.Setup(a => a.GetCredentialByRegistrationNumberAsync("joao@teste.com")).ReturnsAsync(_ativa);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Login("joao@teste.com", SenhaCerta));

        Assert.Equal(AuthAppService.InvalidLoginMessage, error.Message);
        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Senha_errada_roda_o_bcrypt_uma_vez_e_responde_igual()
    {
        _accounts.Setup(a => a.GetCredentialByRegistrationNumberAsync("joao@teste.com")).ReturnsAsync(_ativa);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Login("joao@teste.com", "Senha-Errada-2026"));

        Assert.Equal(AuthAppService.InvalidLoginMessage, error.Message);
        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Senha_certa_devolve_o_token_da_credencial()
    {
        _accounts.Setup(a => a.GetCredentialByRegistrationNumberAsync("joao@teste.com")).ReturnsAsync(_ativa);
        _accounts.Setup(a => a.GetUserByIdAsync("joao")).ReturnsAsync(new User { Id = "joao", Name = "João" });

        var response = await Login("joao@teste.com", SenhaCerta);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("João", response.UserName);
        Assert.Equal(CondominioA, response.CondominiumId);
    }

    // Conta criada pela administração: a credencial tem a matrícula (COND-...) e o login é pelo e-mail.
    // Depois do primeiro acesso, só a credencial tem a senha nova: é ela que vale no login.
    [Fact]
    public async Task Login_por_email_confere_a_senha_da_credencial_depois_do_primeiro_acesso()
    {
        var credencial = new Credential
        {
            UserId = "ana", RegistrationNumber = "COND-2026-1234", Role = "porteiro", CondominiumId = CondominioA,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SenhaCerta, 4), MustChangePassword = false, IsActive = true,
        };
        var usuario = new User
        {
            Id = "ana", Name = "Ana", Email = "ana@teste.com", IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Codigo-Do-Condominio-2026", 4),
        };
        _accounts.Setup(a => a.GetUserByEmailAsync("ana@teste.com")).ReturnsAsync(usuario);
        _accounts.Setup(a => a.GetCredentialByUserIdAsync("ana")).ReturnsAsync(credencial);
        _accounts.Setup(a => a.GetUserByIdAsync("ana")).ReturnsAsync(usuario);

        var response = await Login("Ana@Teste.com", SenhaCerta);

        Assert.Equal("porteiro", response.Role);
        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // Senha legada em texto puro (dados antigos): comparada em tempo constante e com o mesmo custo de BCrypt.
    [Fact]
    public async Task Senha_legada_em_texto_tambem_paga_uma_verificacao_bcrypt()
    {
        var legado = new User { Id = "velho", Name = "Antigo", RegistrationNumber = "COND-2020-0001", IsActive = true, PasswordHash = "texto-antigo-2020" };
        _accounts.Setup(a => a.GetUserByRegistrationNumberIncludingLegacyAsync("COND-2020-0001")).ReturnsAsync(legado);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Login("COND-2020-0001", "outra-senha-2020"));

        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
