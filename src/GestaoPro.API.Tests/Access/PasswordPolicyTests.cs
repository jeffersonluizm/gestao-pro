using GestaoPro.API.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Access;

// Política de senha no servidor (os clientes também avisam, mas quem decide é a API):
// de 8 a 128 caracteres, sem senhas óbvias, sequências ou repetições.
public class PasswordPolicyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Ab1-xyz")]       // 7 caracteres
    [InlineData("12345678")]
    [InlineData("senha123")]
    [InlineData("aaaaaaaa")]
    [InlineData("abcdefgh")]
    public void Senha_curta_ou_obvia_e_fraca(string password)
    {
        Assert.False(PasswordPolicy.IsStrong(password));
    }

    [Fact]
    public void Senha_com_mais_de_128_caracteres_e_recusada()
    {
        Assert.False(PasswordPolicy.IsStrong(new string('x', 60) + "Ab1-" + new string('y', 65)));
    }

    [Theory]
    [InlineData("senha-nova")]
    [InlineData("Jardim-Azul-2026")]
    public void Senha_razoavel_e_aceita(string password)
    {
        Assert.True(PasswordPolicy.IsStrong(password));
    }
}

public class PasswordPolicyEndpointsTests
{
    private const string Fraca = "12345678";

    private readonly Mock<IUserDirectory> _directory = new();
    private readonly Mock<ISelfServiceAccounts> _accounts = new();
    private readonly Mock<IPasswordVerifier> _verifier = new();

    public PasswordPolicyEndpointsTests()
    {
        _directory.Setup(d => d.GetByIdAsync("joao"))
            .ReturnsAsync(new UserResponse { Id = "joao", CondominiumId = CondominioA, Role = UserRole.morador });
    }

    private AuthController Auth() => new(null!, _directory.Object,
        new SelfServiceAuthService(_accounts.Object, _verifier.Object),
        new CondominiumAttemptLimiter(100, TimeSpan.FromMinutes(1)));

    // O serviço de usuários (MongoDB) não é construído: a recusa acontece antes dele.
    private UsersController Users() => new(null!, _directory.Object, NullLogger<UsersController>.Instance);

    private static string? MessageOf(Microsoft.AspNetCore.Mvc.IActionResult result)
    {
        var value = ValueOf<object>(result);
        return value as string ?? value.GetType().GetProperty("message")?.GetValue(value) as string;
    }

    [Fact]
    public async Task Cadastro_anonimo_com_senha_fraca_e_recusado_com_a_regra_sem_tocar_no_banco()
    {
        var result = await Auth().SelfRegister(new SelfRegisterRequest("11111111111", CondominioA, "Jardim-Azul-2026", Fraca, null, null, null));

        Assert.Equal(400, StatusOf(result));
        Assert.Equal(PasswordPolicy.WeakPasswordMessage, MessageOf(result));
        _accounts.VerifyNoOtherCalls();
        _verifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Primeiro_acesso_com_senha_nova_fraca_e_recusado_com_a_regra()
    {
        var result = await Auth().ChangePassword(new AuthChangePasswordRequest("joao", "senha-inicial", Fraca));

        Assert.Equal(400, StatusOf(result));
        Assert.Equal(PasswordPolicy.WeakPasswordMessage, MessageOf(result));
        _accounts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Credencial_criada_pela_administracao_recebe_senha_e_nao_hash()
    {
        Assert.DoesNotContain("PasswordHash", typeof(CreateCredentialRequest).GetProperties().Select(p => p.Name));

        var result = await Auth().As("admin-a", "admin")
            .CreateCredential(new CreateCredentialRequest("joao", "joao@teste.com", Fraca, "morador", CondominioA));

        Assert.Equal(400, StatusOf(result));
        Assert.Equal(PasswordPolicy.WeakPasswordMessage, MessageOf(result));
    }

    [Fact]
    public async Task Troca_da_propria_senha_com_senha_fraca_e_recusada()
    {
        var result = await Users().As("joao", "morador").ChangePassword("joao", new ChangePasswordRequest("atual", Fraca));

        Assert.Equal(400, StatusOf(result));
        Assert.Equal(PasswordPolicy.WeakPasswordMessage, MessageOf(result));
    }

    [Fact]
    public async Task Administracao_nao_define_senha_fraca()
    {
        var controller = Users().As("admin-a", "admin");

        var password = await controller.UpdatePassword("joao", new ResetPasswordRequest(Fraca));
        var credential = await controller.CreateCredentialForExistingUser("joao", new CreateUserCredentialRequest(Fraca));
        var create = await controller.Create(new CreateUserRequest(
            CondominioA, null, null, "Ana", "ana@teste.com", Fraca, "31999999999", "11111111111", UserRole.porteiro, null));

        Assert.All(new[] { password, credential, create }, result =>
        {
            Assert.Equal(400, StatusOf(result));
            Assert.Equal(PasswordPolicy.WeakPasswordMessage, MessageOf(result));
        });
    }

    // Sem senha no cadastro, a conta nasce com o código do condomínio como senha inicial e troca no primeiro acesso.
    [Fact]
    public void Senha_e_opcional_no_cadastro_de_usuario()
    {
        var parameter = typeof(CreateUserRequest).GetConstructors().Single().GetParameters().Single(p => p.Name == "Password");

        Assert.True(new System.Reflection.NullabilityInfoContext().Create(parameter).WriteState is System.Reflection.NullabilityState.Nullable);
    }
}
