using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Users;

// CPF, e-mail e telefone só para o próprio usuário e para a administração do condomínio.
// Os demais logados recebem o resumo (id, nome, unidade, papel) que as telas usam para mostrar nomes.
public class UsersControllerAccessTests
{
    private readonly Mock<IUserDirectory> _directory = new();
    private readonly UsersController _controller;

    private static readonly UserResponse Joao = Make("joao", CondominioA, UserRole.morador);
    private static readonly UserResponse Maria = Make("maria", CondominioA, UserRole.morador);
    private static readonly UserResponse Carlos = Make("carlos", CondominioB, UserRole.morador);
    private static readonly UserResponse Inativo = Make("inativo", CondominioA, UserRole.morador);
    private static readonly UserResponse InativoOutro = Make("inativo-b", CondominioB, UserRole.morador);

    public UsersControllerAccessTests()
    {
        // As escritas (UserAppService) não são exercitadas aqui.
        _controller = new UsersController(null!, _directory.Object, NullLogger<UsersController>.Instance);
        var active = new List<UserResponse> { Joao, Maria, Carlos };
        _directory.Setup(d => d.GetAllActiveAsync()).ReturnsAsync(active);
        _directory.Setup(d => d.GetAllInactiveAsync()).ReturnsAsync(new List<UserResponse> { Inativo, InativoOutro });
        _directory.Setup(d => d.GetByCondominiumAsync(It.IsAny<string>()))
            .ReturnsAsync((string condominiumId) => active.Where(u => u.CondominiumId == condominiumId).ToList());
        _directory.Setup(d => d.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => active.FirstOrDefault(u => u.Id == id));
    }

    [Theory]
    [InlineData("morador")]
    [InlineData("porteiro")]
    public async Task Lista_de_usuarios_para_quem_nao_administra_e_resumo_sem_dados_pessoais(string role)
    {
        var all = await _controller.As("joao", role).GetAllActive();
        var byCondominium = await _controller.As("joao", role).GetByCondominium(CondominioA);

        foreach (var result in new[] { all, byCondominium })
        {
            var users = ValueOf<IEnumerable<UserSummaryResponse>>(result).ToList();
            Assert.Equal(new[] { "joao", "maria" }, users.Select(u => u.Id));
            Assert.Equal("Nome joao", users[0].Name);
        }
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sindico")]
    public async Task Administracao_recebe_dados_completos_so_do_proprio_condominio(string role)
    {
        var result = await _controller.As("gestor", role).GetAllActive();

        var users = ValueOf<IEnumerable<UserResponse>>(result).ToList();
        Assert.Equal(new[] { "joao", "maria" }, users.Select(u => u.Id));
        Assert.Equal("cpf-joao", users[0].Cpf);
    }

    [Fact]
    public async Task Lista_por_condominio_alheio_e_404_para_qualquer_papel()
    {
        var admin = await _controller.As("admin-a", "admin").GetByCondominium(CondominioB);
        var morador = await _controller.As("joao", "morador").GetByCondominium(CondominioB);

        Assert.Equal(404, StatusOf(admin));
        Assert.Equal(404, StatusOf(morador));
    }

    [Fact]
    public async Task Usuario_le_os_proprios_dados_completos()
    {
        var result = await _controller.As("joao", "morador").GetById("joao");

        Assert.Equal("cpf-joao", ValueOf<UserResponse>(result).Cpf);
    }

    [Fact]
    public async Task Morador_lendo_dados_de_outro_morador_recebe_403()
    {
        var result = await _controller.As("joao", "morador").GetById("maria");

        Assert.Equal(403, StatusOf(result));
    }

    [Fact]
    public async Task Admin_le_morador_do_condominio_e_recebe_404_no_de_outro()
    {
        var own = await _controller.As("admin-a", "admin").GetById("maria");
        var other = await _controller.As("admin-a", "admin").GetById("carlos");

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
    }

    [Fact]
    public async Task Inativos_so_para_a_administracao_e_so_do_proprio_condominio()
    {
        var morador = await _controller.As("joao", "morador").GetAllInactive();
        var admin = await _controller.As("admin-a", "admin").GetAllInactive();

        Assert.Equal(403, StatusOf(morador));
        Assert.Equal(new[] { "inativo" }, ValueOf<IEnumerable<UserResponse>>(admin).Select(u => u.Id));
    }

    [Fact]
    public async Task Token_sem_condominio_recebe_403()
    {
        var result = await _controller.As("joao", "morador", condominiumId: null).GetAllActive();

        Assert.Equal(403, StatusOf(result));
        _directory.Verify(d => d.GetAllActiveAsync(), Times.Never);
    }

    private static UserResponse Make(string id, string condominiumId, UserRole role) => new()
    {
        Id = id,
        CondominiumId = condominiumId,
        Name = $"Nome {id}",
        Email = $"{id}@teste.com",
        Phone = $"tel-{id}",
        Cpf = $"cpf-{id}",
        Role = role,
        IsActive = true,
    };
}
