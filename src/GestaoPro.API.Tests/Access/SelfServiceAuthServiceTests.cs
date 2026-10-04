using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Access;

// Rotas anônimas de autoatendimento (check-cpf, register, change-password do primeiro acesso).
// - check-cpf não devolve nome nem diz por que não pode seguir (mensagem única);
// - cadastro só ativa conta de morador e exige o código do condomínio (a senha padrão do condomínio);
// - troca de senha anônima só no primeiro acesso (credencial com mustChangePassword).
public class SelfServiceAuthServiceTests
{
    private const string Codigo = "codigo-do-condominio";
    private const string Cpf = "11111111111";

    private readonly Mock<ISelfServiceAccounts> _accounts = new();
    private readonly SelfServiceAuthService _service;
    private readonly User _morador = new()
    {
        Id = "joao", Name = "João", Cpf = Cpf, CondominiumId = CondominioA, Role = UserRole.morador, IsActive = true,
        Email = "11111111111@pending.gestaopro.local", RegistrationNumber = "PENDING-11111111111",
    };

    public SelfServiceAuthServiceTests()
    {
        _service = new SelfServiceAuthService(_accounts.Object);
        _accounts.Setup(a => a.GetUserByCpfAsync(Cpf)).ReturnsAsync(_morador);
        _accounts.Setup(a => a.GetCondominiumCodeHashAsync(CondominioA)).ReturnsAsync(BCrypt.Net.BCrypt.HashPassword(Codigo));
    }

    // ── check-cpf ──

    [Fact]
    public void Resposta_do_check_cpf_nao_tem_nome_nem_dados_pessoais()
    {
        var properties = typeof(CheckCpfResponse).GetProperties().Select(p => p.Name);

        Assert.Equal(new[] { "CanProceed", "NeedsProfileData", "Message" }, properties);
    }

    [Fact]
    public async Task Morador_sem_acesso_com_codigo_certo_pode_seguir()
    {
        var result = await _service.CheckCpfAsync(new CheckCpfRequest("111.111.111-11", CondominioA, Codigo));

        Assert.True(result.CanProceed);
        Assert.True(result.NeedsProfileData);
    }

    [Fact]
    public async Task Todos_os_motivos_de_recusa_do_check_cpf_tem_a_mesma_resposta()
    {
        var sindico = new User { Id = "s", Cpf = "22222222222", CondominiumId = CondominioA, Role = UserRole.sindico, IsActive = true };
        var comAcesso = new User { Id = "c", Cpf = "33333333333", CondominiumId = CondominioA, Role = UserRole.morador, IsActive = true };
        _accounts.Setup(a => a.GetUserByCpfAsync("22222222222")).ReturnsAsync(sindico);
        _accounts.Setup(a => a.GetUserByCpfAsync("33333333333")).ReturnsAsync(comAcesso);
        _accounts.Setup(a => a.GetCredentialAsync("c")).ReturnsAsync(new Credential { UserId = "c", IsActive = true });

        var results = new[]
        {
            await _service.CheckCpfAsync(new CheckCpfRequest(Cpf, CondominioA, "codigo-errado")),
            await _service.CheckCpfAsync(new CheckCpfRequest(Cpf, CondominioB, Codigo)),
            await _service.CheckCpfAsync(new CheckCpfRequest("99999999999", CondominioA, Codigo)),
            await _service.CheckCpfAsync(new CheckCpfRequest("22222222222", CondominioA, Codigo)),
            await _service.CheckCpfAsync(new CheckCpfRequest("33333333333", CondominioA, Codigo)),
        };

        Assert.All(results, result => Assert.False(result.CanProceed));
        Assert.All(results, result => Assert.False(result.NeedsProfileData));
        Assert.Single(results.Select(result => result.Message).Distinct());
    }

    // ── register ──

    [Fact]
    public async Task Cadastro_de_morador_com_codigo_certo_cria_a_credencial_de_morador()
    {
        Credential? created = null;
        _accounts.Setup(a => a.CreateCredentialAsync(It.IsAny<Credential>())).Callback((Credential c) => created = c);

        var result = await _service.SelfRegisterAsync(
            new SelfRegisterRequest(Cpf, CondominioA, Codigo, "senha-nova", "João da Silva", "joao@teste.com", null));

        Assert.Equal("joao@teste.com", result.RegistrationNumber);
        Assert.NotNull(created);
        Assert.Equal("morador", created!.Role);
        Assert.Equal(CondominioA, created.CondominiumId);
    }

    [Fact]
    public async Task Cadastro_anonimo_nao_ativa_sindico_admin_nem_porteiro()
    {
        foreach (var role in new[] { UserRole.sindico, UserRole.admin, UserRole.porteiro })
        {
            _morador.Role = role;

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SelfRegisterAsync(
                new SelfRegisterRequest(Cpf, CondominioA, Codigo, "senha-nova", null, "joao@teste.com", null)));

            Assert.Equal(SelfServiceAuthService.RegisterRefusedMessage, error.Message);
        }
        _accounts.Verify(a => a.CreateCredentialAsync(It.IsAny<Credential>()), Times.Never);
        _accounts.Verify(a => a.UpdateUserPasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Cadastro_sem_o_codigo_do_condominio_ou_com_cpf_desconhecido_e_recusado_com_a_mesma_mensagem()
    {
        var wrongCode = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SelfRegisterAsync(
            new SelfRegisterRequest(Cpf, CondominioA, "codigo-errado", "senha-nova", null, "joao@teste.com", null)));
        var unknownCpf = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SelfRegisterAsync(
            new SelfRegisterRequest("99999999999", CondominioA, Codigo, "senha-nova", null, "x@teste.com", null)));

        Assert.Equal(SelfServiceAuthService.RegisterRefusedMessage, wrongCode.Message);
        Assert.Equal(SelfServiceAuthService.RegisterRefusedMessage, unknownCpf.Message);
        _accounts.Verify(a => a.CreateCredentialAsync(It.IsAny<Credential>()), Times.Never);
        _accounts.Verify(a => a.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    // ── change-password (primeiro acesso, anônimo) ──

    [Fact]
    public async Task Troca_de_senha_anonima_vale_no_primeiro_acesso()
    {
        _accounts.Setup(a => a.GetCredentialAsync("joao")).ReturnsAsync(new Credential
        {
            UserId = "joao", IsActive = true, MustChangePassword = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("senha-inicial"),
        });

        await _service.ChangePasswordOnFirstAccessAsync(new AuthChangePasswordRequest("joao", "senha-inicial", "senha-nova"));

        _accounts.Verify(a => a.UpdateCredentialPasswordAsync("joao", It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task Troca_de_senha_anonima_fora_do_primeiro_acesso_ou_com_senha_errada_e_recusada_igual()
    {
        _accounts.Setup(a => a.GetCredentialAsync("joao")).ReturnsAsync(new Credential
        {
            UserId = "joao", IsActive = true, MustChangePassword = false, PasswordHash = BCrypt.Net.BCrypt.HashPassword("senha-atual"),
        });
        _accounts.Setup(a => a.GetCredentialAsync("maria")).ReturnsAsync(new Credential
        {
            UserId = "maria", IsActive = true, MustChangePassword = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("senha-inicial"),
        });

        var notFirstAccess = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangePasswordOnFirstAccessAsync(new AuthChangePasswordRequest("joao", "senha-atual", "senha-nova")));
        var wrongPassword = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangePasswordOnFirstAccessAsync(new AuthChangePasswordRequest("maria", "errada", "senha-nova")));
        var unknown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangePasswordOnFirstAccessAsync(new AuthChangePasswordRequest("sumiu", "x", "senha-nova")));

        Assert.Single(new[] { notFirstAccess.Message, wrongPassword.Message, unknown.Message }.Distinct());
        _accounts.Verify(a => a.UpdateCredentialPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }
}
