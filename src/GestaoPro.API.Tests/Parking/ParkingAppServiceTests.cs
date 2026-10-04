namespace GestaoPro.API.Tests.ParkingTests;

using ParkingModel = GestaoPro.API.Models.Parking;
using Parking = GestaoPro.API.Models.Parking;

public class ParkingAppServiceTests
{
    private readonly Mock<IParkingRepository> _repoMock = new();
    private readonly ParkingAppService _service;

    public ParkingAppServiceTests()
    {
        _service = new ParkingAppService(_repoMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaParking()
    {
        var dto = MakeDto();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingModel>()))
                 .ReturnsAsync((ParkingModel p) => { p.Id = "park1"; return p; });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Number.Trim(), result.Number);
        Assert.Equal(dto.Type, result.Type);
        Assert.Equal(dto.CondominiumId, result.CondominiumId);
        Assert.Equal(dto.IsShared, result.IsShared);
    }

    [Fact]
    public async Task CreateAsync_NumeroDuplicado_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        var existente = MakeModel();
        existente.Number = " A-01 "; // com espaços
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { existente });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_NumeroDuplicadoCaseInsensitive_LancaInvalidOperationException()
    {
        var dto = MakeDto();
        dto = new CreateParkingDto
        {
            Number = "a-01",
            Type = dto.Type,
            CondominiumId = dto.CondominiumId,
            IsShared = dto.IsShared,
            UnitId = dto.UnitId
        };
        var existente = MakeModel();
        existente.Number = "A-01";
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { existente });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_TrimNumeroParkingAntes()
    {
        var dto = MakeDto();
        dto = new CreateParkingDto
        {
            Number = "  A-01  ",
            Type = dto.Type,
            CondominiumId = dto.CondominiumId,
            IsShared = dto.IsShared,
            UnitId = dto.UnitId
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingModel>()))
                 .ReturnsAsync((ParkingModel p) => p);

        var result = await _service.CreateAsync(dto);

        Assert.Equal("A-01", result.Number);
    }

    [Fact]
    public async Task CreateAsync_StatusPadraoAvailable()
    {
        var dto = MakeDto();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingModel>()))
                 .ReturnsAsync((ParkingModel p) => p);

        var result = await _service.CreateAsync(dto);

        Assert.Equal("available", result.Status);
    }

    // ── UpdateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ParkingNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Parking?)null);

        var result = await _service.UpdateAsync("id-invalido", new UpdateParkingDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaTipo_RetornaParkingAtualizado()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("park1", It.IsAny<ParkingModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("park1", new UpdateParkingDto { Type = "coberta" });

        Assert.NotNull(result);
        Assert.Equal("coberta", result!.Type);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaNumero_ValidaDuplicacao()
    {
        var existing = MakeModel();
        existing.Id = "park1";
        var outro = MakeModel();
        outro.Id = "park2";
        outro.Number = "A-02";
        
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { outro });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync("park1", new UpdateParkingDto { Number = "A-02" })
        );
    }

    [Fact]
    public async Task UpdateAsync_AtualizaNumeroParaSemelhante_PermiteSeForMesmaParkingId()
    {
        var existing = MakeModel();
        existing.Id = "park1";
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { existing });
        _repoMock.Setup(r => r.UpdateAsync("park1", It.IsAny<ParkingModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("park1", new UpdateParkingDto { Number = "A-01" });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaStatus_RetornaParkingAtualizado()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("park1", It.IsAny<ParkingModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("park1", new UpdateParkingDto { Status = "reserved" });

        Assert.NotNull(result);
        Assert.Equal("reserved", result!.Status);
    }

    [Fact]
    public async Task UpdateAsync_StatusAvailable_LimpaReserva()
    {
        var existing = MakeModel();
        existing.Status = "reserved";
        existing.ReservedByUserId = "user123";
        existing.ReservedAt = DateTime.UtcNow;
        
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("park1", It.IsAny<ParkingModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("park1", new UpdateParkingDto { Status = "available" });

        Assert.NotNull(result);
        Assert.Equal("available", result!.Status);
        Assert.Null(result.ReservedByUserId);
        Assert.Null(result.ReservedAt);
    }

    [Fact]
    public async Task UpdateAsync_ChamaRepositoryUpdate()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("park1", It.IsAny<ParkingModel>()))
                 .Returns(Task.CompletedTask);

        await _service.UpdateAsync("park1", new UpdateParkingDto { Type = "coberta" });

        _repoMock.Verify(r => r.UpdateAsync("park1", It.IsAny<ParkingModel>()), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ChamaRepositoryDelete()
    {
        _repoMock.Setup(r => r.DeleteAsync("park1")).Returns(Task.CompletedTask);

        await _service.DeleteAsync("park1");

        _repoMock.Verify(r => r.DeleteAsync("park1"), Times.Once);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ParkingEncontrada_RetornaParking()
    {
        var parking = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(parking);

        var result = await _service.GetByIdAsync("park1");

        Assert.NotNull(result);
        Assert.Equal(parking.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ParkingNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Parking?)null);

        var result = await _service.GetByIdAsync("id-invalido");

        Assert.Null(result);
    }

    // ── GetAllAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_RetornaTodasAsVagas()
    {
        var parkings = new List<ParkingModel> { MakeModel(), MakeModel() };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(parkings);

        var result = await _service.GetAllAsync();

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    // ── GetAvailableAsync ────────────────────────────────────────

    [Fact]
    public async Task GetAvailableAsync_RetornaVagasDisponiveis()
    {
        var parkings = new List<ParkingModel> { MakeModel() };
        _repoMock.Setup(r => r.GetAvailableAsync()).ReturnsAsync(parkings);

        var result = await _service.GetAvailableAsync();

        Assert.NotEmpty(result);
    }

    // ── ReserveAsync ────────────────────────────────────────────

    [Fact]
    public async Task ReserveAsync_ChamaRepositoryReserve()
    {
        _repoMock.Setup(r => r.ReserveAsync("park1", "user1")).Returns(Task.CompletedTask);

        await _service.ReserveAsync("park1", "user1");

        _repoMock.Verify(r => r.ReserveAsync("park1", "user1"), Times.Once);
    }

    // ── UpdateStatusAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ChamaRepositoryUpdateStatus()
    {
        _repoMock.Setup(r => r.UpdateStatusAsync("park1", "reserved"))
                 .Returns(Task.CompletedTask);

        await _service.UpdateStatusAsync("park1", "reserved");

        _repoMock.Verify(r => r.UpdateStatusAsync("park1", "reserved"), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────

    // O número da vaga é único dentro do condomínio, não entre condomínios.
    [Fact]
    public async Task CreateAsync_MesmoNumeroEmOutroCondominio_Cria()
    {
        var dto = MakeDto();
        var deOutroCondominio = MakeModel();
        deOutroCondominio.CondominiumId = "cond2";
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { deOutroCondominio });
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingModel>()))
                 .ReturnsAsync((ParkingModel p) => { p.Id = "park2"; return p; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal("cond1", result.CondominiumId);
    }

    [Fact]
    public async Task UpdateAsync_NumeroUsadoEmOutroCondominio_Atualiza()
    {
        var existing = MakeModel();
        var deOutroCondominio = MakeModel();
        deOutroCondominio.Id = "park9";
        deOutroCondominio.Number = "B-02";
        deOutroCondominio.CondominiumId = "cond2";
        _repoMock.Setup(r => r.GetByIdAsync("park1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { existing, deOutroCondominio });

        var result = await _service.UpdateAsync("park1", new UpdateParkingDto { Number = "B-02" });

        Assert.Equal("B-02", result!.Number);
    }

    private static CreateParkingDto MakeDto() => new()
    {
        Number = "A-01",
        Type = "descoberta",
        CondominiumId = "cond1",
        IsShared = false,
        UnitId = "unit1"
    };

    private static ParkingModel MakeModel() => new()
    {
        Id = "park1",
        Number = "A-01",
        Type = "descoberta",
        CondominiumId = "cond1",
        Status = "available",
        IsShared = false,
        ResidentId = null,
        UnitId = "unit1",
        ReservedByUserId = null,
        ReservedAt = null,
        CreatedAt = DateTime.UtcNow,
        History = new List<ParkingHistory>()
    };
}



