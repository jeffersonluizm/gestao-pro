using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Access;

// Rotas anônimas de autoatendimento (check-cpf, register, change-password do primeiro acesso).
// - o código do condomínio é conferido ANTES de olhar o CPF, e o BCrypt roda sempre uma vez (hash fixo quando
//   o condomínio não existe): o tempo de resposta não separa "CPF existe" de "não existe";
// - check-cpf não devolve nome nem diz por que não pode seguir (mensagem única);
// - cadastro só ativa conta de morador sem nenhuma credencial (ativa ou desativada) e com código forte;
// - troca de senha anônima só no primeiro acesso (credencial com mustChangePassword).
public class SelfServiceAuthServiceTests
{
    private const string Codigo = "Jardim-Azul-2026";
    private const string Cpf = "11111111111";

    private readonly Mock<ISelfServiceAccounts> _accounts = new();
    private readonly Mock<IPasswordVerifier> _verifier = new();
    private readonly SelfServiceAuthService _service;
    private readonly User _morador = new()
    {
        Id = "joao", Name = "João", Cpf = Cpf, CondominiumId = CondominioA, Role = UserRole.morador, IsActive = true,
        Email = "11111111111@pending.gestaopro.local", RegistrationNumber = "PENDING-11111111111",
    };

    public SelfServiceAuthServiceTests()
    {
        _service = new SelfServiceAuthService(_accounts.Object, _verifier.Object);
        _verifier.Setup(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string text, string hash) => BCrypt.Net.BCrypt.Verify(text, hash));
        _accounts.Setup(a => a.GetUserByCpfAsync(Cpf)).ReturnsAsync(_morador);
        _accounts.Setup(a => a.GetCondominiumCodeHashAsync(CondominioA)).ReturnsAsync(BCrypt.Net.BCrypt.HashPassword(Codigo));
    }

    private void CodeOf(string condominiumId, string code) =>
        _accounts.Setup(a => a.GetCondominiumCodeHashAsync(condominiumId)).ReturnsAsync(BCrypt.Net.BCrypt.HashPassword(code));

    // ── check-cpf ──

    [Fact]
    public void Resposta_do_check_cpf_nao_tem_nome_nem_dados_pessoais()
    {
        var properties = typeof(CheckCpfResponse).GetProperties().Select(p => p.Name);

        Assert.Equal(new[] { "CanProceed", "NeedsProfileData", "Message" }, properties);
    }

    [Fact]
    public async Task Morador_sem_acesso_com_codigo_certo_pode_seguir_e_o_codigo_vale_sem_espacos_nas_pontas()
    {
        var result = await _service.CheckCpfAsync(new CheckCpfRequest("111.111.111-11", CondominioA, $"  {Codigo} "));

        Assert.True(result.CanProceed);
        Assert.True(result.NeedsProfileData);
        _verifier.Verify(v => v.Verify(Codigo, It.IsAny<string>()), Times.Once);
    }

    public static TheoryData<string, string, string> RecusasDoCheckCpf => new()
    {
        { "codigo-errado", Cpf, CondominioA },        // código errado
        { Codigo, Cpf, "condominio-inexistente" },    // condomínio sem código (hash fixo)
        { Codigo, "99999999999", CondominioA },       // CPF desconhecido
        { Codigo, "22222222222", CondominioA },       // síndico
        { Codigo, "33333333333", CondominioA },       // já tem acesso ativo
        { Codigo, "44444444444", CondominioA },       // credencial desativada
        { Codigo, "55555555555", CondominioA },       // morador de outro condomínio
        { "123456", "55555555555", CondominioB },     // condomínio ainda com o código fraco antigo
    };

    [Theory]
    [MemberData(nameof(RecusasDoCheckCpf))]
    public async Task Toda_recusa_do_check_cpf_roda_o_bcrypt_uma_vez_e_tem_a_mesma_resposta(string code, string cpf, string condominiumId)
    {
        SetupRefusalScenarios();

        var result = await _service.CheckCpfAsync(new CheckCpfRequest(cpf, condominiumId, code));

        Assert.False(result.CanProceed);
        Assert.False(result.NeedsProfileData);
        Assert.Equal(SelfServiceAuthService.CheckRefusedMessage, result.Message);
        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Codigo_errado_nem_chega_a_procurar_o_cpf()
    {
        await _service.CheckCpfAsync(new CheckCpfRequest(Cpf, CondominioA, "codigo-errado"));

        _accounts.Verify(a => a.GetUserByCpfAsync(It.IsAny<string>()), Times.Never);
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

    [Theory]
    [MemberData(nameof(RecusasDoCheckCpf))]
    public async Task Toda_recusa_do_cadastro_roda_o_bcrypt_uma_vez_nao_altera_nada_e_tem_a_mesma_mensagem(
        string code, string cpf, string condominiumId)
    {
        SetupRefusalScenarios();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SelfRegisterAsync(
            new SelfRegisterRequest(cpf, condominiumId, code, "senha-nova", "Nome", "x@teste.com", null)));

        Assert.Equal(SelfServiceAuthService.RegisterRefusedMessage, error.Message);
        _verifier.Verify(v => v.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _accounts.Verify(a => a.CreateCredentialAsync(It.IsAny<Credential>()), Times.Never);
        _accounts.Verify(a => a.UpdateCredentialPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _accounts.Verify(a => a.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _accounts.Verify(a => a.UpdateUserPasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

    private void SetupRefusalScenarios()
    {
        _accounts.Setup(a => a.GetCondominiumCodeHashAsync("condominio-inexistente")).ReturnsAsync((string?)null);
        CodeOf(CondominioB, "123456");
        _accounts.Setup(a => a.GetUserByCpfAsync("22222222222")).ReturnsAsync(
            new User { Id = "s", Cpf = "22222222222", CondominiumId = CondominioA, Role = UserRole.sindico, IsActive = true });
        _accounts.Setup(a => a.GetUserByCpfAsync("33333333333")).ReturnsAsync(
            new User { Id = "c", Cpf = "33333333333", CondominiumId = CondominioA, Role = UserRole.morador, IsActive = true });
        _accounts.Setup(a => a.GetCredentialAsync("c")).ReturnsAsync(new Credential { UserId = "c", IsActive = true });
        _accounts.Setup(a => a.GetUserByCpfAsync("44444444444")).ReturnsAsync(
            new User { Id = "d", Cpf = "44444444444", CondominiumId = CondominioA, Role = UserRole.morador, IsActive = true });
        _accounts.Setup(a => a.GetCredentialAsync("d")).ReturnsAsync(new Credential { UserId = "d", IsActive = false });
        _accounts.Setup(a => a.GetUserByCpfAsync("55555555555")).ReturnsAsync(
            new User { Id = "e", Cpf = "55555555555", CondominiumId = CondominioB, Role = UserRole.morador, IsActive = true });
        _accounts.Setup(a => a.GetUserByCpfAsync(Cpf)).ReturnsAsync(_morador);
    }
}
