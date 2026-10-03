namespace GestaoPro.API.Tests.ReservationTests;

using ReservationModel = GestaoPro.API.Models.Reservation;
using CommonAreaModel = GestaoPro.API.Models.CommonArea;
using Reservation = GestaoPro.API.Models.Reservation;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _repoMock = new();
    private readonly Mock<ICommonAreaRepository> _commonAreaRepoMock = new();
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _service = new ReservationService(_repoMock.Object, _commonAreaRepoMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaReservation()
    {
        var dto = MakeDto();
        var commonArea = MakeCommonArea();
        var reservationRetornada = MakeModel();
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(dto.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>()))
                 .ReturnsAsync(reservationRetornada);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.CommonAreaId, result.CommonAreaId);
        Assert.Equal(dto.UserId, result.UserId);
        Assert.Equal(dto.Date, result.Date);
    }

    [Fact]
    public async Task CreateAsync_CommonAreaNaoEncontrada_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync((CommonAreaModel?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_CommonAreaInativa_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        var commonArea = MakeCommonArea();
        commonArea.IsActive = false;
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_DataPassada_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        dto.Date = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var commonArea = MakeCommonArea();
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_HorarioInvalido_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        dto.StartTime = "15:00";
        dto.EndTime = "14:00"; // end < start
        var commonArea = MakeCommonArea();
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_HorarioForaDoExpediente_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        dto.StartTime = "07:00"; // antes do opening (08:00)
        var commonArea = MakeCommonArea();
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_HorarioComConflito_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        var commonArea = MakeCommonArea();
        var reservaExistente = MakeModel();
        reservaExistente.Date = dto.Date;
        reservaExistente.StartTime = "10:00";
        reservaExistente.EndTime = "12:00";
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(dto.CommonAreaId))
                 .ReturnsAsync(new List<ReservationModel> { reservaExistente });

        // dto.StartTime = "11:00", dto.EndTime = "13:00" - conflita com 10:00-12:00
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_StatusPadrao_RetornaReservationComStatusPendente()
    {
        var dto = MakeDto();
        var commonArea = MakeCommonArea();
        var reservationRetornada = MakeModel();
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(dto.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>()))
                 .ReturnsAsync(reservationRetornada);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(ReservationStatus.Pendente, result.Status);
    }

    [Fact]
    public async Task CreateAsync_UsaCondominiumIdDaCommonAreaSeVazio()
    {
        var dto = MakeDto();
        dto.CondominiumId = string.Empty;
        var commonArea = MakeCommonArea();
        var reservationRetornada = MakeModel();
        
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(dto.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(dto.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>()))
                 .ReturnsAsync(reservationRetornada);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(commonArea.CondominiumId, result.CondominiumId);
    }

    // ── UpdateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReservationNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((ReservationModel?)null);

        var result = await _service.UpdateAsync("id-invalido", new UpdateReservationDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaStatus_RetornaReservationAtualizada()
    {
        var existing = MakeModel();
        var commonArea = MakeCommonArea();
        
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(existing);
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(existing.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(existing.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.UpdateAsync("res1", It.IsAny<Reservation>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("res1", new UpdateReservationDto { Status = ReservationStatus.Aprovada });

        Assert.NotNull(result);
        Assert.Equal(ReservationStatus.Aprovada, result!.Status);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaData_RetornaReservationAtualizada()
    {
        var existing = MakeModel();
        var commonArea = MakeCommonArea();
        var novaData = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd");
        
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(existing);
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(existing.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(existing.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.UpdateAsync("res1", It.IsAny<Reservation>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("res1", new UpdateReservationDto { Date = novaData });

        Assert.NotNull(result);
        Assert.Equal(novaData, result!.Date);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaHorarios_RetornaReservationAtualizada()
    {
        var existing = MakeModel();
        var commonArea = MakeCommonArea();
        
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(existing);
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(existing.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(existing.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.UpdateAsync("res1", It.IsAny<Reservation>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("res1", new UpdateReservationDto
        {
            StartTime = "14:00",
            EndTime = "16:00"
        });

        Assert.NotNull(result);
        Assert.Equal("14:00", result!.StartTime);
        Assert.Equal("16:00", result!.EndTime);
    }

    [Fact]
    public async Task UpdateAsync_ValoresNulos_NaoAlteraAtributos()
    {
        var existing = MakeModel();
        var commonArea = MakeCommonArea();
        var originalDate = existing.Date;
        
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(existing);
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(existing.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(existing.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.UpdateAsync("res1", It.IsAny<Reservation>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("res1", new UpdateReservationDto());

        Assert.NotNull(result);
        Assert.Equal(originalDate, result!.Date);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaTimestampUpdatedAt()
    {
        var existing = MakeModel();
        var commonArea = MakeCommonArea();
        var beforeCall = DateTime.UtcNow;
        
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(existing);
        _commonAreaRepoMock.Setup(r => r.GetByIdAsync(existing.CommonAreaId))
                           .ReturnsAsync(commonArea);
        _repoMock.Setup(r => r.GetByCommonAreaAsync(existing.CommonAreaId))
                 .ReturnsAsync(new List<Reservation>());
        _repoMock.Setup(r => r.UpdateAsync("res1", It.IsAny<Reservation>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("res1", new UpdateReservationDto { Status = ReservationStatus.Aprovada });

        Assert.NotNull(result);
        Assert.InRange(result!.UpdatedAt, beforeCall, DateTime.UtcNow);
    }

    // ── DeleteAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReservationNaoEncontrada_RetornaFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((ReservationModel?)null);

        var result = await _service.DeleteAsync("id-invalido");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ReservationEncontrada_RetornaTrue()
    {
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(MakeModel());
        _repoMock.Setup(r => r.DeleteAsync("res1")).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync("res1");

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync("res1"), Times.Once);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReservationEncontrada_RetornaReservation()
    {
        var reservation = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("res1")).ReturnsAsync(reservation);

        var result = await _service.GetByIdAsync("res1");

        Assert.NotNull(result);
        Assert.Equal(reservation.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReservationNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Reservation?)null);

        var result = await _service.GetByIdAsync("id-invalido");

        Assert.Null(result);
    }

    // ── GetAllAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_RetornaTodasAsReservas()
    {
        var reservations = new List<ReservationModel> { MakeModel(), MakeModel() };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(reservations);

        var result = await _service.GetAllAsync();

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    // ── GetByCondominiumAsync ────────────────────────────────────

    [Fact]
    public async Task GetByCondominiumAsync_RetornaReservasPorCondominio()
    {
        var reservations = new List<ReservationModel> { MakeModel() };
        _repoMock.Setup(r => r.GetByCondominiumAsync("cond1")).ReturnsAsync(reservations);

        var result = await _service.GetByCondominiumAsync("cond1");

        Assert.NotEmpty(result);
    }

    // ── GetByUserAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_RetornaReservasDoUsuario()
    {
        var reservations = new List<ReservationModel> { MakeModel() };
        _repoMock.Setup(r => r.GetByUserAsync("user1")).ReturnsAsync(reservations);

        var result = await _service.GetByUserAsync("user1");

        Assert.NotEmpty(result);
    }

    // ── GetByCommonAreaAsync ─────────────────────────────────────

    [Fact]
    public async Task GetByCommonAreaAsync_RetornaReservasPorArea()
    {
        var reservations = new List<ReservationModel> { MakeModel() };
        _repoMock.Setup(r => r.GetByCommonAreaAsync("area1")).ReturnsAsync(reservations);

        var result = await _service.GetByCommonAreaAsync("area1");

        Assert.NotEmpty(result);
    }

    // ── GetByStatusAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_RetornaReservasPorStatus()
    {
        var reservations = new List<ReservationModel> { MakeModel() };
        _repoMock.Setup(r => r.GetByStatusAsync(ReservationStatus.Pendente))
                 .ReturnsAsync(reservations);

        var result = await _service.GetByStatusAsync(ReservationStatus.Pendente);

        Assert.NotEmpty(result);
    }

    // ── GetByDateAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetByDateAsync_RetornaReservasPorData()
    {
        var reservations = new List<ReservationModel> { MakeModel() };
        _repoMock.Setup(r => r.GetByDateAsync(It.IsAny<string>()))
                 .ReturnsAsync(reservations);

        var result = await _service.GetByDateAsync("2026-06-05");

        Assert.NotEmpty(result);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static CreateReservationDto MakeDto() => new()
    {
        CommonAreaId = "area1",
        UserId = "user1",
        CondominiumId = "cond1",
        Date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
        StartTime = "11:00",
        EndTime = "13:00",
        Observations = "Primeira reserva"
    };

    private static ReservationModel MakeModel() => new()
    {
        Id = "res1",
        CommonAreaId = "area1",
        UserId = "user1",
        CondominiumId = "cond1",
        Date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
        StartTime = "11:00",
        EndTime = "13:00",
        Status = ReservationStatus.Pendente,
        Observations = "Primeira reserva",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CommonAreaModel MakeCommonArea() => new()
    {
        Id = "area1",
        CondominiumId = "cond1",
        Name = "Piscina",
        Description = "Piscina",
        Capacity = 50,
        OpenTime = "08:00",
        CloseTime = "22:00",
        Rules = string.Empty,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
