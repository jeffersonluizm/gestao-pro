using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Users;

public class UsersControllerWriteTests
{
    private readonly Mock<IUserDirectory> _directory = new();

    public UsersControllerWriteTests()
    {
        var users = new List<UserResponse>
        {
            new() { Id = "joao", CondominiumId = CondominioA, Name = "João", Role = UserRole.morador },
            new() { Id = "carlos", CondominiumId = CondominioB, Name = "Carlos", Role = UserRole.morador },
            new() { Id = CarlosId, CondominiumId = CondominioB, Name = "Carlos", Role = UserRole.morador },
        };
        _directory.Setup(d => d.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => users.FirstOrDefault(u => u.Id == id));
    }

    private const string CarlosId = "665f1c2e9b1e8a00000000c2";

    // As escritas que chegam ao UserAppService não são exercitadas aqui (dependem do MongoDB);
    // os testes cobrem as recusas, que acontecem antes de qualquer acesso ao serviço.
    private UsersController Controller() => new(null!, _directory.Object, NullLogger<UsersController>.Instance);

    // A troca de senha exige a senha atual (UserAppService.ChangePasswordAsync), mas só vale para a própria conta:
    // nem admin troca a de outro por aqui (para isso existe o reset-password).
    [Theory]
    [InlineData("morador")]
    [InlineData("porteiro")]
    [InlineData("admin")]
    public async Task Trocar_a_senha_de_outro_id_e_403(string role)
    {
        var result = await Controller().As("maria", role)
            .ChangePassword("joao", new ChangePasswordRequest("atual", "nova-senha"));

        Assert.Equal(403, StatusOf(result));
    }

    // Admin/síndico só cadastra e altera usuários do próprio condomínio; o de outro responde 404.
    [Fact]
    public async Task Admin_nao_cadastra_usuario_em_outro_condominio()
    {
        var result = await Controller().As("admin-a", "admin").Create(new CreateUserRequest(
            CondominioB, null, null, "Carlos", "carlos@teste.com", "123456", "31999999999", "11111111111", UserRole.morador, null));

        Assert.Equal(404, StatusOf(result));
    }

    [Fact]
    public async Task Admin_nao_altera_desativa_nem_mexe_na_senha_de_usuario_de_outro_condominio()
    {
        var controller = Controller().As("admin-a", "admin");

        var update = await controller.Update(CarlosId, new UpdateUserRequest("Outro", null, null, null, null, null, null, null, null));
        var deactivate = await controller.Deactivate(CarlosId);
        var reset = await controller.ResetPassword(CarlosId);
        var password = await controller.UpdatePassword(CarlosId, new ResetPasswordRequest("nova-senha"));
        var credential = await controller.CreateCredentialForExistingUser(CarlosId, new CreateUserCredentialRequest("nova-senha"));

        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(deactivate));
        Assert.Equal(404, StatusOf(reset));
        Assert.Equal(404, StatusOf(password));
        Assert.Equal(404, StatusOf(credential));
    }
}
