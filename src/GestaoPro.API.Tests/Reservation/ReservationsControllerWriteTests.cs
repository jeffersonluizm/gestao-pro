using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;
using ReservationModel = GestaoPro.API.Models.Reservation;

namespace GestaoPro.API.Tests.ReservationTests;

public class ReservationsControllerWriteTests
{
    private readonly Mock<IReservationService> _service = new();
    private readonly ReservationsController _controller;

    public ReservationsControllerWriteTests()
    {
        _controller = new ReservationsController(_service.Object);
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
}
