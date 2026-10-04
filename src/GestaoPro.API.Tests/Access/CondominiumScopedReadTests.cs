using GestaoPro.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;
using AnnouncementModel = GestaoPro.API.Models.Announcement;
using ParkingModel = GestaoPro.API.Models.Parking;

namespace GestaoPro.API.Tests.Access;

// Leituras de dados do condomínio (vagas, áreas comuns, avisos, assembleias, unidades, condomínio):
// só do condomínio do token; de outro condomínio, 404 (item) ou fora da lista.

public class ParkingControllerReadTests
{
    private readonly Mock<IParkingRepository> _repository = new();
    private readonly ParkingController _controller;

    public ParkingControllerReadTests()
    {
        _controller = new ParkingController(new ParkingAppService(_repository.Object), new Mock<IUserDirectory>().Object);
        var all = new List<ParkingModel>
        {
            new()
            {
                Id = "vaga-joao", Number = "A-01", CondominiumId = CondominioA, ResidentId = "joao", ReservedByUserId = "joao",
                History = new List<ParkingHistory> { new() { Action = "reserve", UserId = "joao" } },
            },
            new()
            {
                Id = "vaga-maria", Number = "A-02", CondominiumId = CondominioA, ResidentId = "maria", ReservedByUserId = "maria",
                History = new List<ParkingHistory> { new() { Action = "reserve", UserId = "maria" } },
            },
            new() { Id = "vaga-b", Number = "B-01", CondominiumId = CondominioB, ResidentId = "carlos" },
        };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(all);
        _repository.Setup(r => r.GetAvailableAsync()).ReturnsAsync(all);
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(p => p.Id == id));
    }

    [Fact]
    public async Task Morador_ve_as_vagas_do_condominio_sem_dados_de_outros_moradores()
    {
        var result = await _controller.As("joao", "morador").Get();

        var parkings = ValueOf<List<ParkingModel>, IEnumerable<ParkingModel>>(result).ToList();
        Assert.Equal(new[] { "vaga-joao", "vaga-maria" }, parkings.Select(p => p.Id));
        Assert.Equal("joao", parkings[0].ResidentId);
        Assert.Equal("joao", parkings[0].ReservedByUserId);
        Assert.Null(parkings[1].ResidentId);
        Assert.Null(parkings[1].ReservedByUserId);
        Assert.All(parkings[1].History, entry => Assert.Null(entry.UserId));
    }

    [Fact]
    public async Task Admin_ve_dados_completos_e_vaga_de_outro_condominio_e_404()
    {
        var all = await _controller.As("admin-a", "admin").GetAvailable();
        var other = await _controller.As("admin-a", "admin").GetById("vaga-b");

        var parkings = ValueOf<List<ParkingModel>, IEnumerable<ParkingModel>>(all).ToList();
        Assert.Equal("maria", parkings[1].ResidentId);
        Assert.Equal(404, StatusOf(other));
    }

    [Fact]
    public async Task Morador_le_vaga_de_outro_morador_sem_os_dados_dele()
    {
        var result = await _controller.As("joao", "morador").GetById("vaga-maria");

        var parking = ValueOf<ParkingModel, ParkingModel>(result);
        Assert.Null(parking.ResidentId);
    }
}

public class CommonAreasControllerReadTests
{
    private readonly Mock<ICommonAreaService> _service = new();
    private readonly CommonAreasController _controller;

    public CommonAreasControllerReadTests()
    {
        _controller = new CommonAreasController(_service.Object);
        var all = new List<CommonArea>
        {
            new() { Id = "area-a", CondominiumId = CondominioA },
            new() { Id = "area-b", CondominiumId = CondominioB },
        };
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((string id) => all.FirstOrDefault(a => a.Id == id));
    }

    [Fact]
    public async Task Lista_so_as_areas_do_condominio_e_404_nas_de_outro()
    {
        var all = await _controller.As("joao", "morador").GetAll();
        var other = await _controller.As("joao", "morador").GetById("area-b");
        var byOther = await _controller.As("admin-a", "admin").GetByCondominium(CondominioB);

        Assert.Equal(new[] { "area-a" }, ValueOf<IEnumerable<CommonArea>>(all).Select(a => a.Id));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(byOther));
    }
}

public class AnnouncementsControllerReadTests
{
    private readonly Mock<IAnnouncementService> _service = new();
    private readonly AnnouncementsController _controller;

    public AnnouncementsControllerReadTests()
    {
        _controller = new AnnouncementsController(_service.Object);
        var all = new List<AnnouncementModel>
        {
            new() { Id = "aviso-a", CondominiumId = CondominioA, Category = "geral" },
            new() { Id = "aviso-b", CondominiumId = CondominioB, Category = "geral" },
        };
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetActiveAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetByCategoryAsync("geral")).ReturnsAsync(all);
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((string id) => all.FirstOrDefault(a => a.Id == id));
    }

    [Fact]
    public async Task Avisos_so_do_condominio_e_404_nos_de_outro()
    {
        var all = await _controller.As("joao", "morador").GetAll();
        var active = await _controller.As("joao", "morador").GetActive();
        var category = await _controller.As("joao", "morador").GetByCategory("geral");
        var other = await _controller.As("joao", "morador").GetById("aviso-b");
        var byOther = await _controller.As("joao", "morador").GetByCondominium(CondominioB);

        foreach (var list in new[] { all, active, category })
            Assert.Equal(new[] { "aviso-a" }, ValueOf<IEnumerable<AnnouncementModel>>(list).Select(a => a.Id));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(byOther));
    }
}

public class AssemblyControllerReadTests
{
    private readonly Mock<IAssemblyService> _service = new();
    private readonly AssemblyController _controller;

    public AssemblyControllerReadTests()
    {
        _controller = new AssemblyController(_service.Object, NullLogger<AssemblyController>.Instance);
        var all = new List<AssemblyModel>
        {
            new() { Id = "assembleia-a", CondominiumId = CondominioA, Title = "A" },
            new() { Id = "assembleia-b", CondominiumId = CondominioB, Title = "B" },
        };
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetHistoryAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((string id) => all.FirstOrDefault(a => a.Id == id));
    }

    [Fact]
    public async Task Assembleias_so_do_condominio_e_404_nas_de_outro()
    {
        var all = await _controller.As("joao", "morador").GetAll();
        var history = await _controller.As("joao", "morador").GetAll(history: true);
        var other = await _controller.As("joao", "morador").GetById("assembleia-b");
        var byOther = await _controller.As("admin-a", "admin").GetByCondominium(CondominioB);

        Assert.Equal(new[] { "assembleia-a" }, ValueOf<IEnumerable<AssemblyModel>>(all).Select(a => a.Id));
        Assert.Equal(new[] { "assembleia-a" }, ValueOf<IEnumerable<AssemblyModel>>(history).Select(a => a.Id));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(byOther));
    }
}

public class UnitsControllerReadTests
{
    private readonly Mock<IUnitService> _service = new();
    private readonly UnitsController _controller;

    public UnitsControllerReadTests()
    {
        _controller = new UnitsController(_service.Object, new Mock<IUserDirectory>().Object);
        var unitA = Unit("unidade-a", CondominioA, "joao", "ana");
        var unitA2 = Unit("unidade-a2", CondominioA, "maria");
        _service.Setup(s => s.GetAllByCondominiumAsync(CondominioA)).ReturnsAsync(new List<UnitResponse> { unitA, unitA2 });
        _service.Setup(s => s.GetByIdAsync("unidade-a")).ReturnsAsync(unitA);
        _service.Setup(s => s.GetByIdAsync("unidade-b")).ReturnsAsync(Unit("unidade-b", CondominioB, "carlos"));
    }

    [Fact]
    public async Task Morador_ve_as_unidades_sem_os_ids_dos_outros_moradores()
    {
        var result = await _controller.As("joao", "morador").GetAllByCondominium(CondominioA);

        var units = ValueOf<IEnumerable<UnitResponse>>(result).ToList();
        Assert.Equal(new[] { "joao" }, units[0].ResidentIds);
        Assert.Empty(units[1].ResidentIds);
        Assert.Equal(2, units[0].ResidentCount);
    }

    [Fact]
    public async Task Admin_ve_os_moradores_e_unidade_de_outro_condominio_e_404()
    {
        var own = await _controller.As("admin-a", "admin").GetById("unidade-a");
        var other = await _controller.As("admin-a", "admin").GetById("unidade-b");
        var byOther = await _controller.As("admin-a", "admin").GetAllByCondominium(CondominioB);

        Assert.Equal(new[] { "joao", "ana" }, ValueOf<UnitResponse>(own).ResidentIds);
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(byOther));
    }

    private static UnitResponse Unit(string id, string condominiumId, params string[] residents) =>
        new(id, condominiumId, "101", null, 1, default, residents.ToList(), residents.Length, null, null, DateTime.UtcNow);
}

public class CondominiumsControllerReadTests
{
    private readonly Mock<ICondominiumService> _service = new();
    private readonly CondominiumsController _controller;

    public CondominiumsControllerReadTests()
    {
        _controller = new CondominiumsController(_service.Object);
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((string id) => new CondominiumResponse { Id = id });
    }

    // Tela de cadastro (anônima): só id e nome, sem CNPJ, endereço nem contagem de moradores.
    [Fact]
    public async Task Lista_publica_tem_so_id_e_nome()
    {
        _service.Setup(s => s.GetPublicAsync()).ReturnsAsync(new List<CondominiumSummary>
        {
            new(CondominioA, "Residencial A"),
            new(CondominioB, "Residencial B"),
        });

        var result = await _controller.GetPublic();

        var list = ValueOf<IEnumerable<CondominiumSummary>>(result).ToList();
        Assert.Equal(new[] { "Residencial A", "Residencial B" }, list.Select(c => c.Name));
        Assert.Equal(new[] { "Id", "Name" }, typeof(CondominiumSummary).GetProperties().Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task Lista_com_login_traz_so_o_proprio_condominio()
    {
        var result = await _controller.As("admin-a", "admin").GetAll();

        var list = ValueOf<IEnumerable<CondominiumResponse>>(result).ToList();
        Assert.Equal(new[] { CondominioA }, list.Select(c => c.Id));
    }

    [Fact]
    public async Task Lista_com_token_sem_condominio_e_recusada()
    {
        var result = await _controller.As("admin-a", "admin", condominiumId: null).GetAll();

        Assert.Equal(403, StatusOf(result));
    }

    [Fact]
    public async Task Detalhe_so_do_proprio_condominio()
    {
        var own = await _controller.As("joao", "morador").GetById(CondominioA);
        var other = await _controller.As("joao", "morador").GetById(CondominioB);

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
    }
}
