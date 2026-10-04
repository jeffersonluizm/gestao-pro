using GestaoPro.API.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;
using AnnouncementModel = GestaoPro.API.Models.Announcement;

namespace GestaoPro.API.Tests.Access;

// Recursos do próprio condomínio (áreas comuns, avisos, assembleias, unidades, condomínio):
// admin/síndico só cria, altera e remove no condomínio do token; outro condomínio responde 404.

public class CommonAreasControllerWriteTests
{
    private readonly Mock<ICommonAreaService> _service = new();
    private readonly CommonAreasController _controller;

    public CommonAreasControllerWriteTests()
    {
        _controller = new CommonAreasController(_service.Object);
        _service.Setup(s => s.GetByIdAsync("area-a")).ReturnsAsync(new CommonArea { Id = "area-a", CondominiumId = CondominioA });
        _service.Setup(s => s.GetByIdAsync("area-b")).ReturnsAsync(new CommonArea { Id = "area-b", CondominiumId = CondominioB });
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Admin_so_altera_areas_do_proprio_condominio()
    {
        var create = await _controller.As("admin-a", "admin").Create(new CreateCommonAreaDto { CondominiumId = CondominioB, Name = "Piscina" });
        var update = await _controller.As("admin-a", "admin").Update("area-b", new UpdateCommonAreaDto { Name = "Outra" });
        var delete = await _controller.As("admin-a", "admin").Delete("area-b");
        var ownDelete = await _controller.As("admin-a", "admin").Delete("area-a");

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateCommonAreaDto>()), Times.Never);
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateCommonAreaDto>()), Times.Never);
        _service.Verify(s => s.DeleteAsync("area-b"), Times.Never);
    }
}

public class AnnouncementsControllerWriteTests
{
    private readonly Mock<IAnnouncementService> _service = new();
    private readonly AnnouncementsController _controller;

    public AnnouncementsControllerWriteTests()
    {
        _controller = new AnnouncementsController(_service.Object);
        _service.Setup(s => s.GetByIdAsync("aviso-a")).ReturnsAsync(new AnnouncementModel { Id = "aviso-a", CondominiumId = CondominioA });
        _service.Setup(s => s.GetByIdAsync("aviso-b")).ReturnsAsync(new AnnouncementModel { Id = "aviso-b", CondominiumId = CondominioB });
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Sindico_so_altera_avisos_do_proprio_condominio()
    {
        var create = await _controller.As("sindico-a", "sindico").Create(new CreateAnnouncementDto { CondominiumId = CondominioB, Title = "Aviso" });
        var update = await _controller.As("sindico-a", "sindico").Update("aviso-b", new UpdateAnnouncementDto { Title = "Outro" });
        var delete = await _controller.As("sindico-a", "sindico").Delete("aviso-b");
        var ownDelete = await _controller.As("sindico-a", "sindico").Delete("aviso-a");

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateAnnouncementDto>()), Times.Never);
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateAnnouncementDto>()), Times.Never);
    }
}

public class AssemblyControllerWriteTests
{
    private readonly Mock<IAssemblyService> _service = new();
    private readonly AssemblyController _controller;

    public AssemblyControllerWriteTests()
    {
        _controller = new AssemblyController(_service.Object, NullLogger<AssemblyController>.Instance);
        _service.Setup(s => s.GetByIdAsync("assembleia-a")).ReturnsAsync(new AssemblyModel { Id = "assembleia-a", CondominiumId = CondominioA, Title = "A" });
        _service.Setup(s => s.GetByIdAsync("assembleia-b")).ReturnsAsync(new AssemblyModel { Id = "assembleia-b", CondominiumId = CondominioB, Title = "B" });
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Admin_so_altera_assembleias_do_proprio_condominio()
    {
        var create = await _controller.As("admin-a", "admin").Create(new CreateAssemblyDto { CondominiumId = CondominioB, Title = "AGO" });
        var update = await _controller.As("admin-a", "admin").Update("assembleia-b", new UpdateAssemblyDto { Title = "Outra" });
        var delete = await _controller.As("admin-a", "admin").Delete("assembleia-b");
        var ownDelete = await _controller.As("admin-a", "admin").Delete("assembleia-a");

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateAssemblyDto>()), Times.Never);
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateAssemblyDto>()), Times.Never);
        _service.Verify(s => s.DeleteAsync("assembleia-b", It.IsAny<string?>()), Times.Never);
    }
}

public class UnitsControllerWriteTests
{
    private readonly Mock<IUnitService> _service = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly UnitsController _controller;

    public UnitsControllerWriteTests()
    {
        _controller = new UnitsController(_service.Object, _users.Object);
        _service.Setup(s => s.GetByIdAsync("unidade-a")).ReturnsAsync(MakeUnit("unidade-a", CondominioA));
        _service.Setup(s => s.GetByIdAsync("unidade-b")).ReturnsAsync(MakeUnit("unidade-b", CondominioB));
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _service.Setup(s => s.AddResidentAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _users.Setup(u => u.GetByIdAsync("joao")).ReturnsAsync(new UserResponse { Id = "joao", CondominiumId = CondominioA });
        _users.Setup(u => u.GetByIdAsync("carlos")).ReturnsAsync(new UserResponse { Id = "carlos", CondominiumId = CondominioB });
    }

    [Fact]
    public async Task Admin_so_altera_unidades_do_proprio_condominio()
    {
        var create = await _controller.As("admin-a", "admin").Create(new CreateUnitRequest(CondominioB, "101", null, 1, default));
        var update = await _controller.As("admin-a", "admin").Update("unidade-b", new UpdateUnitRequest("102", null, null, null));
        var delete = await _controller.As("admin-a", "admin").Delete("unidade-b");
        var ownDelete = await _controller.As("admin-a", "admin").Delete("unidade-a");

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateUnitRequest>()), Times.Never);
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateUnitRequest>()), Times.Never);
    }

    [Fact]
    public async Task Vinculo_de_morador_so_entre_unidade_e_morador_do_proprio_condominio()
    {
        var own = await _controller.As("admin-a", "admin").AddResident("unidade-a", "joao");
        var otherResident = await _controller.As("admin-a", "admin").AddResident("unidade-a", "carlos");
        var otherUnit = await _controller.As("admin-a", "admin").RemoveResident("unidade-b", "carlos");

        Assert.Equal(204, StatusOf(own));
        Assert.Equal(404, StatusOf(otherResident));
        Assert.Equal(404, StatusOf(otherUnit));
        _service.Verify(s => s.AddResidentAsync("unidade-a", "carlos"), Times.Never);
        _service.Verify(s => s.RemoveResidentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static UnitResponse MakeUnit(string id, string condominiumId) =>
        new(id, condominiumId, "101", null, 1, default, new List<string>(), 0, null, null, DateTime.UtcNow);
}

public class CondominiumsControllerWriteTests
{
    private readonly Mock<ICondominiumService> _service = new();
    private readonly CondominiumsController _controller;

    public CondominiumsControllerWriteTests()
    {
        _controller = new CondominiumsController(_service.Object);
        _service.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateCondominiumRequest>()))
            .ReturnsAsync(new CondominiumResponse { Id = CondominioA });
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Admin_so_altera_e_remove_o_proprio_condominio()
    {
        var request = new UpdateCondominiumRequest("Novo nome", null, null, null, null, null, null, null, null, null);
        var own = await _controller.As("admin-a", "admin").Update(CondominioA, request);
        var other = await _controller.As("admin-a", "admin").Update(CondominioB, request);
        var delete = await _controller.As("admin-a", "admin").Delete(CondominioB);

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(delete));
        _service.Verify(s => s.UpdateAsync(CondominioB, It.IsAny<UpdateCondominiumRequest>()), Times.Never);
        _service.Verify(s => s.DeleteAsync(CondominioB), Times.Never);
    }
}
