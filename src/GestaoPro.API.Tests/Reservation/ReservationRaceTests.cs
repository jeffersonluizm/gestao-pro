using System.Collections.Concurrent;

namespace GestaoPro.API.Tests.ReservationTests;

using CommonAreaModel = GestaoPro.API.Models.CommonArea;
using Reservation = GestaoPro.API.Models.Reservation;

// Corrida: vários pedidos simultâneos para o mesmo horário da mesma área. A conferência de conflito e a
// gravação precisam acontecer juntas (trava por área e data); só um pedido pode ganhar.
public class ReservationRaceTests
{
    private readonly ConcurrentBag<Reservation> _stored = new();
    private readonly Mock<IReservationRepository> _repository = new();
    private readonly Mock<ICommonAreaRepository> _areas = new();

    public ReservationRaceTests()
    {
        _areas.Setup(a => a.GetByIdAsync("area1")).ReturnsAsync(new CommonAreaModel
        {
            Id = "area1", CondominiumId = "cond1", Name = "Salão", OpenTime = "08:00", CloseTime = "22:00", IsActive = true,
        });
        // Banco lento de propósito: alarga a janela entre a leitura e a gravação.
        _repository.Setup(r => r.GetByCommonAreaAsync("area1")).Returns(async () =>
        {
            var snapshot = _stored.ToList();
            await Task.Delay(30);
            return snapshot;
        });
        _repository.Setup(r => r.CreateAsync(It.IsAny<Reservation>())).Returns(async (Reservation reservation) =>
        {
            await Task.Delay(10);
            reservation.Id = Guid.NewGuid().ToString("N");
            _stored.Add(reservation);
            return reservation;
        });
    }

    private static CreateReservationDto Pedido(string userId) => new()
    {
        CommonAreaId = "area1",
        UserId = userId,
        CondominiumId = "cond1",
        Date = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),
        StartTime = "18:00",
        EndTime = "20:00",
    };

    [Fact]
    public async Task Pedidos_simultaneos_no_mesmo_horario_criam_uma_reserva_so()
    {
        var service = new ReservationService(_repository.Object, _areas.Object, new InProcessReservationSlotLock());

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async i =>
        {
            try { await service.CreateAsync(Pedido($"morador-{i}")); return true; }
            catch (InvalidOperationException) { return false; }
        }));

        Assert.Equal(1, results.Count(ok => ok));
        Assert.Single(_stored);
    }

    [Fact]
    public async Task Areas_diferentes_nao_esperam_uma_pela_outra()
    {
        var slotLock = new InProcessReservationSlotLock();

        await using var first = await slotLock.AcquireAsync("area1", "2026-12-01");
        var other = slotLock.AcquireAsync("area2", "2026-12-01").AsTask();

        Assert.True(other.Wait(TimeSpan.FromSeconds(1)));
        await (await other).DisposeAsync();
    }
}
