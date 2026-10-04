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
        };
        _directory.Setup(d => d.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => users.FirstOrDefault(u => u.Id == id));
    }

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
}
