using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;
using VisitorModel = GestaoPro.API.Models.Visitor;

namespace GestaoPro.API.Tests.VisitorTests;

public class VisitorControllerWriteTests
{
    private readonly Mock<IVisitorRepository> _repository = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly VisitorController _controller;

    public VisitorControllerWriteTests()
    {
        _controller = new VisitorController(new VisitorAppService(_repository.Object), _users.Object, NullLogger<VisitorController>.Instance);
        var all = new List<VisitorModel>
        {
            new() { Id = "665f1c2e9b1e8a0000000a01", ResidentId = "joao", CondominiumId = CondominioA },
            new() { Id = "665f1c2e9b1e8a0000000b01", ResidentId = "carlos", CondominiumId = CondominioB },
        };
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(v => v.Id == id));
        _repository.Setup(r => r.CreateAsync(It.IsAny<VisitorModel>()))
            .ReturnsAsync((VisitorModel visitor) => { visitor.Id = "novo"; return visitor; });
        foreach (var (id, condominiumId) in new[] { ("joao", CondominioA), ("maria", CondominioA), ("carlos", CondominioB) })
        {
            _users.Setup(u => u.GetByIdAsync(id)).ReturnsAsync(new UserResponse { Id = id, CondominiumId = condominiumId });
        }
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

    // Cadastro: morador registra só para si (o X-User-Id não decide mais); portaria e administração
    // registram para morador do próprio condomínio, sempre gravando o condomínio do token.
    [Fact]
    public async Task Morador_registra_visitante_sempre_para_si_no_proprio_condominio()
    {
        var controller = _controller.As("joao", "morador");
        controller.Request.Headers["X-User-Id"] = "maria";

        var result = await controller.Register(new CreateVisitorDto { FullName = "Visita", ResidentId = "maria", CondominiumId = CondominioB });

        Assert.Equal(201, StatusOf(result));
        _repository.Verify(r => r.CreateAsync(It.Is<VisitorModel>(v => v.ResidentId == "joao" && v.CondominiumId == CondominioA)), Times.Once);
    }

    [Fact]
    public async Task Porteiro_registra_visitante_para_morador_do_condominio_e_404_para_o_de_outro()
    {
        var controller = _controller.As("porteiro-a", "porteiro");
        controller.Request.Headers["X-User-Id"] = "maria";
        var own = await controller.Register(new CreateVisitorDto { FullName = "Visita" });
        controller.Request.Headers["X-User-Id"] = "carlos";
        var other = await controller.Register(new CreateVisitorDto { FullName = "Visita" });

        Assert.Equal(201, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        _repository.Verify(r => r.CreateAsync(It.Is<VisitorModel>(v => v.ResidentId == "maria" && v.CondominiumId == CondominioA)), Times.Once);
        _repository.Verify(r => r.CreateAsync(It.Is<VisitorModel>(v => v.ResidentId == "carlos")), Times.Never);
    }

    [Fact]
    public async Task Admin_altera_e_remove_so_visitante_do_proprio_condominio()
    {
        var controller = _controller.As("admin-a", "admin");

        var update = await controller.Update(DoCondominioB, new UpdateVisitorDto { FullName = "Outro" });
        var moveAway = await controller.Update(DoCondominioA, new UpdateVisitorDto { CondominiumId = CondominioB });
        var otherResident = await controller.Update(DoCondominioA, new UpdateVisitorDto { ResidentId = "carlos" });
        var delete = await controller.Delete(DoCondominioB);
        var ownDelete = await controller.Delete(DoCondominioA);

        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(moveAway));
        Assert.Equal(404, StatusOf(otherResident));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<VisitorModel>()), Times.Never);
        _repository.Verify(r => r.DeleteAsync(DoCondominioB), Times.Never);
    }
}
