using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;
using VisitorModel = GestaoPro.API.Models.Visitor;

namespace GestaoPro.API.Tests.VisitorTests;

// Visitante tem documento e telefone: morador vê os próprios; portaria e administração, os do condomínio.
public class VisitorControllerAccessTests
{
    private readonly Mock<IVisitorRepository> _repository = new();
    private readonly VisitorController _controller;

    private static readonly VisitorModel DoJoao = Make("visitante-joao", "joao", CondominioA);
    private static readonly VisitorModel DaMaria = Make("visitante-maria", "maria", CondominioA);
    private static readonly VisitorModel DeOutroCondominio = Make("visitante-carlos", "carlos", CondominioB);

    public VisitorControllerAccessTests()
    {
        _controller = new VisitorController(new VisitorAppService(_repository.Object), new Mock<IUserDirectory>().Object, NullLogger<VisitorController>.Instance);
        var all = new List<VisitorModel> { DoJoao, DaMaria, DeOutroCondominio };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(all);
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(v => v.Id == id));
        _repository.Setup(r => r.GetByResidentAsync(It.IsAny<string>()))
            .ReturnsAsync((string residentId) => all.Where(v => v.ResidentId == residentId).ToList());
    }

    [Fact]
    public async Task Morador_lista_so_os_proprios_visitantes()
    {
        var result = await _controller.As("joao", "morador").Get();

        Assert.Equal(new[] { "visitante-joao" }, Ids(result));
    }

    [Theory]
    [InlineData("porteiro")]
    [InlineData("admin")]
    public async Task Portaria_e_administracao_listam_os_visitantes_do_proprio_condominio(string role)
    {
        var result = await _controller.As("funcionario", role).Get();

        Assert.Equal(new[] { "visitante-joao", "visitante-maria" }, Ids(result));
    }

    [Fact]
    public async Task Morador_lendo_visitante_de_outro_recebe_403()
    {
        var byId = await _controller.As("joao", "morador").GetById("visitante-maria");
        var byResident = await _controller.As("joao", "morador").GetByResident("maria");

        Assert.Equal(403, StatusOf(byId));
        Assert.Equal(403, StatusOf(byResident));
    }

    [Fact]
    public async Task Morador_le_o_proprio_visitante()
    {
        var byId = await _controller.As("joao", "morador").GetById("visitante-joao");
        var byResident = await _controller.As("joao", "morador").GetByResident("joao");

        Assert.Equal(200, StatusOf(byId));
        Assert.Equal(new[] { "visitante-joao" }, Ids(byResident));
    }

    [Fact]
    public async Task Porteiro_de_outro_condominio_recebe_404_e_lista_vazia()
    {
        var byId = await _controller.As("porteiro-b", "porteiro", CondominioB).GetById("visitante-joao");
        var byResident = await _controller.As("porteiro-b", "porteiro", CondominioB).GetByResident("joao");

        Assert.Equal(404, StatusOf(byId));
        Assert.Empty(Ids(byResident));
    }

    [Fact]
    public async Task Token_sem_condominio_recebe_403()
    {
        var result = await _controller.As("admin-a", "admin", condominiumId: null).Get();

        Assert.Equal(403, StatusOf(result));
        _repository.Verify(r => r.GetAllAsync(), Times.Never);
    }

    private static IEnumerable<string?> Ids<T>(Microsoft.AspNetCore.Mvc.ActionResult<T> result) =>
        ValueOf<T, IEnumerable<VisitorModel>>(result).Select(v => v.Id);

    private static VisitorModel Make(string id, string residentId, string condominiumId) => new()
    {
        Id = id,
        ResidentId = residentId,
        CondominiumId = condominiumId,
        FullName = "Visitante",
        Document = "000",
    };
}
