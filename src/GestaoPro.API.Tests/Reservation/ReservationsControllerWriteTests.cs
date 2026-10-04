using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;
using ReservationModel = GestaoPro.API.Models.Reservation;

namespace GestaoPro.API.Tests.ReservationTests;

public class ReservationsControllerWriteTests
{
    private readonly Mock<IReservationService> _service = new();
    private readonly Mock<ICommonAreaService> _areas = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly ReservationsController _controller;

    public ReservationsControllerWriteTests()
    {
        _controller = new ReservationsController(_service.Object, _areas.Object, _users.Object);
        _areas.Setup(a => a.GetByIdAsync("area-1")).ReturnsAsync(new CommonArea { Id = "area-1", CondominiumId = CondominioA });
        _areas.Setup(a => a.GetByIdAsync("area-b")).ReturnsAsync(new CommonArea { Id = "area-b", CondominiumId = CondominioB });
        _users.Setup(u => u.GetByIdAsync("maria")).ReturnsAsync(new UserResponse { Id = "maria", CondominiumId = CondominioA });
        _users.Setup(u => u.GetByIdAsync("carlos")).ReturnsAsync(new UserResponse { Id = "carlos", CondominiumId = CondominioB });
        var all = new List<ReservationModel>
        {
            new() { Id = "reserva-joao", UserId = "joao", CondominiumId = CondominioA },
            new() { Id = "reserva-maria", UserId = "maria", CondominiumId = CondominioA },
            new() { Id = "reserva-carlos", UserId = "carlos", CondominiumId = CondominioB },
        };
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(r => r.Id == id));
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    // O app mobile mostra "Remover" nas reservas do próprio morador.
    [Fact]
    public async Task Morador_remove_a_propria_reserva()
    {
        var result = await _controller.As("joao", "morador").Delete("reserva-joao");

        Assert.Equal(204, StatusOf(result));
    }

    [Fact]
    public async Task Morador_nao_remove_reserva_de_outro()
    {
        var result = await _controller.As("joao", "morador").Delete("reserva-maria");

        Assert.Equal(403, StatusOf(result));
        _service.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Admin_remove_do_proprio_condominio_e_recebe_404_no_de_outro()
    {
        var own = await _controller.As("admin-a", "admin").Delete("reserva-maria");
        var other = await _controller.As("admin-a", "admin").Delete("reserva-carlos");

        Assert.Equal(204, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        _service.Verify(s => s.DeleteAsync("reserva-carlos"), Times.Never);
    }

    [Fact]
    public async Task Morador_cria_reserva_sempre_em_nome_proprio_e_no_proprio_condominio()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateReservationDto>()))
            .ReturnsAsync(new ReservationModel { Id = "nova", UserId = "joao", CondominiumId = CondominioA });
        var dto = new CreateReservationDto { CommonAreaId = "area-1", UserId = "maria", CondominiumId = CondominioB, Date = "2026-10-10", StartTime = "10:00", EndTime = "12:00" };

        var result = await _controller.As("joao", "morador").Create(dto);

        Assert.Equal(201, StatusOf(result));
        Assert.Equal("joao", dto.UserId);
        Assert.Equal(CondominioA, dto.CondominiumId);
    }

    [Fact]
    public async Task Admin_nao_cria_nem_altera_reserva_de_outro_condominio()
    {
        var create = await _controller.As("admin-a", "admin").Create(
            new CreateReservationDto { CommonAreaId = "area-9", UserId = "carlos", CondominiumId = CondominioB, Date = "2026-10-10", StartTime = "10:00", EndTime = "12:00" });
        var update = await _controller.As("admin-a", "admin").Update("reserva-carlos", new UpdateReservationDto { Status = "aprovada" });

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateReservationDto>()), Times.Never);
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateReservationDto>()), Times.Never);
    }

    // A área comum define o condomínio da reserva: área de outro condomínio não existe para quem reserva.
    // Sem isso, o conflito de horário (400) revelava a ocupação e a reserva bloqueava a área do outro condomínio.
    [Fact]
    public async Task Morador_nao_reserva_area_comum_de_outro_condominio()
    {
        var result = await _controller.As("joao", "morador").Create(
            new CreateReservationDto { CommonAreaId = "area-b", Date = "2026-10-10", StartTime = "10:00", EndTime = "12:00" });

        Assert.Equal(404, StatusOf(result));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateReservationDto>()), Times.Never);
    }

    [Fact]
    public async Task Admin_reserva_area_do_condominio_para_morador_do_condominio_e_grava_o_condominio_da_area()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateReservationDto>()))
            .ReturnsAsync(new ReservationModel { Id = "nova", UserId = "maria", CondominiumId = CondominioA });
        var dto = new CreateReservationDto { CommonAreaId = "area-1", UserId = "maria", CondominiumId = "", Date = "2026-10-10", StartTime = "10:00", EndTime = "12:00" };

        var own = await _controller.As("admin-a", "admin").Create(dto);
        var otherArea = await _controller.As("admin-a", "admin").Create(
            new CreateReservationDto { CommonAreaId = "area-b", UserId = "maria", CondominiumId = CondominioA, Date = "2026-10-10", StartTime = "10:00", EndTime = "12:00" });
        var otherResident = await _controller.As("admin-a", "admin").Create(
            new CreateReservationDto { CommonAreaId = "area-1", UserId = "carlos", CondominiumId = CondominioA, Date = "2026-10-10", StartTime = "10:00", EndTime = "12:00" });

        Assert.Equal(201, StatusOf(own));
        Assert.Equal(CondominioA, dto.CondominiumId);
        Assert.Equal(404, StatusOf(otherArea));
        Assert.Equal(404, StatusOf(otherResident));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateReservationDto>()), Times.Once);
    }
}
