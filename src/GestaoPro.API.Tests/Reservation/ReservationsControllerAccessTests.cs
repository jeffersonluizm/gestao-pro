using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.AspNetCore.Mvc;
using static GestaoPro.API.Tests.Access.TestSession;
using ReservationModel = GestaoPro.API.Models.Reservation;

namespace GestaoPro.API.Tests.ReservationTests;

// Reserva diz quem usa qual área e quando: morador vê só as próprias, administração só as do próprio condomínio.
public class ReservationsControllerAccessTests
{
    private readonly Mock<IReservationService> _service = new();
    private readonly ReservationsController _controller;

    private static readonly ReservationModel DoJoao = Make("reserva-joao", "joao", CondominioA);
    private static readonly ReservationModel DaMaria = Make("reserva-maria", "maria", CondominioA);
    private static readonly ReservationModel DeOutroCondominio = Make("reserva-carlos", "carlos", CondominioB);

    public ReservationsControllerAccessTests()
    {
        _controller = new ReservationsController(_service.Object, new Mock<ICommonAreaService>().Object, new Mock<IUserDirectory>().Object);
        var all = new List<ReservationModel> { DoJoao, DaMaria, DeOutroCondominio };
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(r => r.Id == id));
        _service.Setup(s => s.GetByUserAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) => all.Where(r => r.UserId == userId).ToList());
        _service.Setup(s => s.GetByCondominiumAsync(It.IsAny<string>()))
            .ReturnsAsync((string condominiumId) => all.Where(r => r.CondominiumId == condominiumId).ToList());
        _service.Setup(s => s.GetByCommonAreaAsync(It.IsAny<string>())).ReturnsAsync(all);
        _service.Setup(s => s.GetByStatusAsync(It.IsAny<string>())).ReturnsAsync(all);
        _service.Setup(s => s.GetByDateAsync(It.IsAny<string>())).ReturnsAsync(all);
    }

    [Fact]
    public async Task Morador_le_a_propria_reserva_e_recebe_403_na_de_outro()
    {
        var own = await _controller.As("joao", "morador").GetById("reserva-joao");
        var other = await _controller.As("joao", "morador").GetById("reserva-maria");

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(403, StatusOf(other));
    }

    [Fact]
    public async Task Admin_de_outro_condominio_recebe_404_na_reserva()
    {
        var result = await _controller.As("admin-b", "admin", CondominioB).GetById("reserva-joao");

        Assert.Equal(404, StatusOf(result));
    }

    [Fact]
    public async Task Morador_listando_reservas_de_outro_recebe_403()
    {
        var result = await _controller.As("joao", "morador").GetByUser("maria");

        Assert.Equal(403, StatusOf(result));
    }

    [Fact]
    public async Task Admin_lista_reservas_so_do_proprio_condominio()
    {
        var all = await _controller.As("admin-a", "admin").GetAll();
        var byOtherCondominiumUser = await _controller.As("admin-a", "admin").GetByUser("carlos");

        Assert.Equal(new[] { "reserva-joao", "reserva-maria" }, Ids(all));
        Assert.Empty(Ids(byOtherCondominiumUser));
    }

    [Fact]
    public async Task Lista_por_condominio_alheio_e_404_e_morador_ve_so_as_proprias_do_seu()
    {
        var otherCondominium = await _controller.As("admin-a", "admin").GetByCondominium(CondominioB);
        var ownCondominium = await _controller.As("joao", "morador").GetByCondominium(CondominioA);

        Assert.Equal(404, StatusOf(otherCondominium));
        Assert.Equal(new[] { "reserva-joao" }, Ids(ownCondominium));
    }

    [Fact]
    public async Task Filtros_por_area_status_e_data_devolvem_so_o_que_o_chamador_pode_ver()
    {
        var byArea = await _controller.As("joao", "morador").GetByCommonArea("area-1");
        var byStatus = await _controller.As("porteiro-1", "porteiro").GetByStatus("pendente");
        var byDate = await _controller.As("sindico-a", "sindico").GetByDate("2026-10-04");

        Assert.Equal(new[] { "reserva-joao" }, Ids(byArea));
        Assert.Empty(Ids(byStatus));
        Assert.Equal(new[] { "reserva-joao", "reserva-maria" }, Ids(byDate));
    }

    [Fact]
    public async Task Token_sem_condominio_recebe_403_em_vez_de_todas_as_reservas()
    {
        var result = await _controller.As("joao", "morador", condominiumId: null).GetAll();

        Assert.Equal(403, StatusOf(result));
        _service.Verify(s => s.GetAllAsync(), Times.Never);
    }

    private static IEnumerable<string?> Ids(IActionResult result) =>
        ValueOf<IEnumerable<ReservationModel>>(result).Select(r => r.Id);

    private static ReservationModel Make(string id, string userId, string condominiumId) => new()
    {
        Id = id,
        UserId = userId,
        CondominiumId = condominiumId,
        CommonAreaId = "area-1",
        Date = "2026-10-04",
        StartTime = "10:00",
        EndTime = "12:00",
    };
}
