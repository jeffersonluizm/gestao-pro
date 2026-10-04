using GestaoPro.API.Controllers;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Access;

// Credenciais (criar, resetar senha, desativar) são escritas da administração: só de usuário do próprio condomínio.
public class AuthControllerAccessTests
{
    private readonly Mock<IUserDirectory> _users = new();

    public AuthControllerAccessTests()
    {
        _users.Setup(u => u.GetByIdAsync("carlos")).ReturnsAsync(new UserResponse { Id = "carlos", CondominiumId = CondominioB });
    }

    // O AuthAppService não é exercitado aqui (depende do MongoDB): as recusas acontecem antes dele.
    private AuthController Controller() => new AuthController(null!, _users.Object).As("admin-a", "admin");

    [Fact]
    public async Task Admin_nao_mexe_na_credencial_de_usuario_de_outro_condominio()
    {
        var create = await Controller().CreateCredential(new CreateCredentialRequest("carlos", "carlos@teste.com", "hash", "morador", CondominioB));
        var reset = await Controller().ResetPassword(new AuthResetPasswordRequest("carlos", "nova-senha"));
        var deactivate = await Controller().Deactivate("carlos");
        var unknown = await Controller().Deactivate("nao-existe");

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(reset));
        Assert.Equal(404, StatusOf(deactivate));
        Assert.Equal(404, StatusOf(unknown));
    }
}
