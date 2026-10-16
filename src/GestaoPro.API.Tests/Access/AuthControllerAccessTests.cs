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
    private AuthController Controller() => new AuthController(null!, _users.Object, null!, new CondominiumAttemptLimiter(100, TimeSpan.FromMinutes(1))).As("admin-a", "admin");

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

    // Rotas anônimas: qualquer falha responde a mensagem única da rota, nunca o texto da exceção.
    [Fact]
    public async Task Autoatendimento_nunca_devolve_o_texto_da_excecao()
    {
        var accounts = new Mock<ISelfServiceAccounts>();
        accounts.Setup(a => a.GetCondominiumCodeHashAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("detalhe interno do banco"));
        accounts.Setup(a => a.GetCredentialAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("detalhe interno do banco"));
        var controller = new AuthController(null!, _users.Object,
            new SelfServiceAuthService(accounts.Object, new BcryptPasswordVerifier()),
            new CondominiumAttemptLimiter(100, TimeSpan.FromMinutes(1)));

        var check = await controller.CheckCpf(new CheckCpfRequest("11111111111", CondominioA, "Jardim-Azul-2026"));
        var register = await controller.SelfRegister(new SelfRegisterRequest("11111111111", CondominioA, "Jardim-Azul-2026", "Senha-Forte-2026", null, null, null));
        var change = await controller.ChangePassword(new AuthChangePasswordRequest("joao", "x", "Senha-Forte-2026"));

        Assert.Equal(SelfServiceAuthService.CheckRefusedMessage, MessageOf(check));
        Assert.Equal(SelfServiceAuthService.RegisterRefusedMessage, MessageOf(register));
        Assert.Equal(SelfServiceAuthService.ChangePasswordRefusedMessage, MessageOf(change));
        Assert.All(new[] { check, register, change }, result => Assert.Equal(400, StatusOf(result)));
    }

    private static string? MessageOf(Microsoft.AspNetCore.Mvc.IActionResult result)
    {
        var value = ValueOf<object>(result);
        return value.GetType().GetProperty("message")?.GetValue(value) as string;
    }
}
