using GestaoPro.API.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Users;

// Erro inesperado (banco, driver, bug) não devolve o texto da exceção ao cliente.
public class UnexpectedErrorMessageTests
{
    [Fact]
    public async Task Falha_inesperada_no_cadastro_responde_mensagem_generica()
    {
        // Serviço ausente: qualquer chamada estoura uma exceção que não é regra de negócio.
        var controller = new UsersController(null!, new Mock<IUserDirectory>().Object, NullLogger<UsersController>.Instance).As("admin-a", "admin");

        var result = await controller.Create(new CreateUserRequest(
            CondominioA, null, null, "Ana", "ana@teste.com", "Senha-Forte-2026", "3133330000", "12345678901", UserRole.morador, null));

        var value = ValueOf<object>(result);
        var message = value.GetType().GetProperty("message")?.GetValue(value) as string;
        Assert.DoesNotContain("Object reference", message);
        Assert.DoesNotContain("instância", message ?? string.Empty);
        Assert.Equal(UsersController.UnexpectedErrorMessage, message);
    }
}
