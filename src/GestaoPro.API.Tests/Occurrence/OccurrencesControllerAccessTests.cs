using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.OccurrenceTests;

public class OccurrencesControllerAccessTests
{
    private readonly Mock<IOccurrenceService> _service = new();
    private readonly OccurrencesController _controller;

    private static readonly OccurrenceResponse DoJoao = Make("ocorrencia-joao", "joao", CondominioA);
    private static readonly OccurrenceResponse DaMaria = Make("ocorrencia-maria", "maria", CondominioA);
    private static readonly OccurrenceResponse DeOutroCondominio = Make("ocorrencia-carlos", "carlos", CondominioB);

    public OccurrencesControllerAccessTests()
    {
        _controller = new OccurrencesController(_service.Object);
        var all = new List<OccurrenceResponse> { DoJoao, DaMaria, DeOutroCondominio };
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(o => o.Id == id));
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    // O app mobile mostra "Remover" nas ocorrências do próprio morador (e não manda X-User-Id).
    [Fact]
    public async Task Morador_remove_a_propria_ocorrencia()
    {
        var result = await _controller.As("joao", "morador").Delete("ocorrencia-joao");

        Assert.Equal(204, StatusOf(result));
        _service.Verify(s => s.DeleteAsync("ocorrencia-joao"), Times.Once);
    }

    [Fact]
    public async Task Morador_nao_remove_ocorrencia_de_outro()
    {
        var result = await _controller.As("joao", "morador").Delete("ocorrencia-maria");

        Assert.Equal(403, StatusOf(result));
        _service.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Admin_remove_do_proprio_condominio_e_recebe_404_no_de_outro()
    {
        var own = await _controller.As("admin-a", "admin").Delete("ocorrencia-maria");
        var other = await _controller.As("admin-a", "admin").Delete("ocorrencia-carlos");

        Assert.Equal(204, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        _service.Verify(s => s.DeleteAsync("ocorrencia-carlos"), Times.Never);
    }

    private static OccurrenceResponse Make(string id, string userId, string condominiumId) => new()
    {
        Id = id,
        UserId = userId,
        CondominiumId = condominiumId,
        Title = "Barulho",
    };
}
