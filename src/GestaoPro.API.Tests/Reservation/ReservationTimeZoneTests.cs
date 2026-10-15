namespace GestaoPro.API.Tests.ReservationTests;

using CommonAreaModel = GestaoPro.API.Models.CommonArea;
using Reservation = GestaoPro.API.Models.Reservation;

// O servidor hospedado roda em UTC. "Hoje" para a reserva é o dia de Brasília: entre 21h e 23h59 de Brasília
// o relógio UTC já está no dia seguinte, e reservar para o mesmo dia não pode ser recusado.
public class ReservationTimeZoneTests
{
    // 15/10/2026 às 01:30 UTC = 14/10/2026 às 22:30 em Brasília.
    private static readonly TimeProvider NoiteEmBrasilia = new RelogioFixo(new DateTimeOffset(2026, 10, 15, 1, 30, 0, TimeSpan.Zero));

    private readonly Mock<IReservationRepository> _repository = new();
    private readonly Mock<ICommonAreaRepository> _areas = new();

    public ReservationTimeZoneTests()
    {
        _areas.Setup(a => a.GetByIdAsync("area1")).ReturnsAsync(new CommonAreaModel
        {
            Id = "area1", CondominiumId = "cond1", Name = "Salão", OpenTime = "08:00", CloseTime = "23:59", IsActive = true,
        });
        _repository.Setup(r => r.GetByCommonAreaAsync("area1")).ReturnsAsync(new List<Reservation>());
        _repository.Setup(r => r.CreateAsync(It.IsAny<Reservation>())).ReturnsAsync((Reservation r) => r);
    }

    private ReservationService Service() =>
        new(_repository.Object, _areas.Object, new InProcessReservationSlotLock(), NoiteEmBrasilia);

    private static CreateReservationDto Pedido(string date) => new()
    {
        CommonAreaId = "area1", UserId = "morador", CondominiumId = "cond1", Date = date, StartTime = "23:00", EndTime = "23:30",
    };

    [Fact]
    public async Task Reserva_para_hoje_em_Brasilia_e_aceita_mesmo_com_o_UTC_no_dia_seguinte()
    {
        var reservation = await Service().CreateAsync(Pedido("2026-10-14"));

        Assert.Equal("2026-10-14", reservation.Date);
    }

    [Fact]
    public async Task Reserva_para_ontem_em_Brasilia_continua_recusada()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Service().CreateAsync(Pedido("2026-10-13")));

        Assert.Contains("data anterior a hoje", error.Message);
    }

    // Aprovar ou cancelar uma reserva antiga muda só o status: a data não é conferida de novo.
    [Fact]
    public async Task Mudanca_so_de_status_nao_confere_a_data()
    {
        _repository.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(new Reservation
        {
            Id = "r1", CommonAreaId = "area1", UserId = "morador", CondominiumId = "cond1",
            Date = "2026-10-01", StartTime = "10:00", EndTime = "12:00", Status = ReservationStatus.Pendente,
        });

        var updated = await Service().UpdateAsync("r1", new UpdateReservationDto { Status = ReservationStatus.Cancelada });

        Assert.Equal(ReservationStatus.Cancelada, updated!.Status);
        _repository.Verify(r => r.UpdateAsync("r1", It.IsAny<Reservation>()), Times.Once);
    }

    [Fact]
    public async Task Mudar_a_data_para_o_passado_continua_recusado()
    {
        _repository.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(new Reservation
        {
            Id = "r1", CommonAreaId = "area1", UserId = "morador", CondominiumId = "cond1",
            Date = "2026-10-20", StartTime = "10:00", EndTime = "12:00", Status = ReservationStatus.Pendente,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Service().UpdateAsync("r1", new UpdateReservationDto { Date = "2026-10-13" }));
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
