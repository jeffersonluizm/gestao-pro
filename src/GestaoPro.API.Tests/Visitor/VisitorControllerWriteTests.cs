using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;
using VisitorModel = GestaoPro.API.Models.Visitor;

namespace GestaoPro.API.Tests.VisitorTests;

public class VisitorControllerWriteTests
{
    private readonly Mock<IVisitorRepository> _repository = new();
    private readonly VisitorController _controller;

    public VisitorControllerWriteTests()
    {
        _controller = new VisitorController(new VisitorAppService(_repository.Object), NullLogger<VisitorController>.Instance);
        var all = new List<VisitorModel>
        {
            new() { Id = "665f1c2e9b1e8a0000000a01", ResidentId = "joao", CondominiumId = CondominioA },
            new() { Id = "665f1c2e9b1e8a0000000b01", ResidentId = "carlos", CondominiumId = CondominioB },
        };
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(v => v.Id == id));
    }

    private const string DoCondominioA = "665f1c2e9b1e8a0000000a01";
    private const string DoCondominioB = "665f1c2e9b1e8a0000000b01";

    [Theory]
    [InlineData("porteiro")]
    [InlineData("sindico")]
    public async Task Portaria_registra_entrada_e_saida_de_visitante_do_proprio_condominio(string role)
    {
        var checkIn = await _controller.As("funcionario", role).CheckIn(DoCondominioA);
        var checkOut = await _controller.As("funcionario", role).CheckOut(DoCondominioA);

        Assert.Equal(204, StatusOf(checkIn));
        Assert.Equal(204, StatusOf(checkOut));
        _repository.Verify(r => r.CheckInAsync(DoCondominioA), Times.Once);
        _repository.Verify(r => r.CheckOutAsync(DoCondominioA), Times.Once);
    }

    [Fact]
    public async Task Porteiro_de_outro_condominio_recebe_404()
    {
        var checkIn = await _controller.As("porteiro-a", "porteiro").CheckIn(DoCondominioB);
        var checkOut = await _controller.As("porteiro-a", "porteiro").CheckOut(DoCondominioB);

        Assert.Equal(404, StatusOf(checkIn));
        Assert.Equal(404, StatusOf(checkOut));
        _repository.Verify(r => r.CheckInAsync(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.CheckOutAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Morador_nao_registra_entrada_nem_do_proprio_visitante()
    {
        var result = await _controller.As("joao", "morador").CheckIn(DoCondominioA);

        Assert.Equal(403, StatusOf(result));
    }
}
