namespace GestaoPro.API.Tests.CommonAreaTests;

using CommonAreaModel = GestaoPro.API.Models.CommonArea;
using CommonArea = GestaoPro.API.Models.CommonArea;

public class CommonAreaServiceTests
{
    private readonly Mock<ICommonAreaRepository> _repoMock = new();
    private readonly CommonAreaService _service;

    public CommonAreaServiceTests()
    {
        _service = new CommonAreaService(_repoMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaCommonArea()
    {
        var dto = MakeDto();
        var commonAreaRetornada = MakeModel();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<CommonAreaModel>()))
                 .ReturnsAsync(commonAreaRetornada);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.CondominiumId, result.CondominiumId);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.Capacity, result.Capacity);
    }

    [Fact]
    public async Task CreateAsync_UsaHorariosForecidos()
    {
        var dto = MakeDto();
        dto.OpenTime = "09:00";
        dto.CloseTime = "21:00";
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<CommonAreaModel>()))
                 .ReturnsAsync((CommonAreaModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("09:00", result.OpenTime);
        Assert.Equal("21:00", result.CloseTime);
    }

    [Fact]
    public async Task CreateAsync_UsaRegrasForecidas()
    {
        var dto = MakeDto();
        dto.Rules = "Uso apenas de segunda a sexta";
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<CommonAreaModel>()))
                 .ReturnsAsync((CommonAreaModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Rules, result.Rules);
    }

    [Fact]
    public async Task CreateAsync_StatusAtivoPorPadrao()
    {
        var dto = MakeDto();
        var commonAreaRetornada = MakeModel();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<CommonAreaModel>()))
                 .ReturnsAsync(commonAreaRetornada);

        var result = await _service.CreateAsync(dto);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_CriaComTimestamps()
    {
        var dto = MakeDto();
        var beforeCall = DateTime.UtcNow;
        var commonAreaRetornada = MakeModel();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<CommonAreaModel>()))
                 .ReturnsAsync(commonAreaRetornada);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result.CreatedAt);
        Assert.NotNull(result.UpdatedAt);
    }

    // ── UpdateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_CommonAreaNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((CommonArea?)null);

        var result = await _service.UpdateAsync("id-invalido", new UpdateCommonAreaDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaNome_RetornaCommonAreaAtualizada()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("area1", new UpdateCommonAreaDto { Name = "Nova Área" });

        Assert.NotNull(result);
        Assert.Equal("Nova Área", result!.Name);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaCapacidade_RetornaCommonAreaAtualizada()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("area1", new UpdateCommonAreaDto { Capacity = 100 });

        Assert.NotNull(result);
        Assert.Equal(100, result!.Capacity);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaHorarios_RetornaCommonAreaAtualizada()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("area1", new UpdateCommonAreaDto
        {
            OpenTime = "10:00",
            CloseTime = "20:00"
        });

        Assert.NotNull(result);
        Assert.Equal("10:00", result!.OpenTime);
        Assert.Equal("20:00", result!.CloseTime);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaStatus_RetornaCommonAreaAtualizada()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("area1", new UpdateCommonAreaDto { IsActive = false });

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ValoresNulos_NaoAlteraAtributos()
    {
        var existing = MakeModel();
        var originalName = existing.Name;
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("area1", new UpdateCommonAreaDto());

        Assert.NotNull(result);
        Assert.Equal(originalName, result!.Name);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaTimestampUpdatedAt()
    {
        var existing = MakeModel();
        var beforeCall = DateTime.UtcNow;
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("area1", new UpdateCommonAreaDto { Name = "Novo Nome" });

        Assert.NotNull(result);
        Assert.InRange(result!.UpdatedAt, beforeCall, DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_ChamaRepositoryUpdate()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()))
                 .Returns(Task.CompletedTask);

        await _service.UpdateAsync("area1", new UpdateCommonAreaDto { Name = "Novo Nome" });

        _repoMock.Verify(r => r.UpdateAsync("area1", It.IsAny<CommonAreaModel>()), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_CommonAreaNaoEncontrada_RetornaFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((CommonArea?)null);

        var result = await _service.DeleteAsync("id-invalido");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_CommonAreaEncontrada_RetornaTrue()
    {
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(MakeModel());
        _repoMock.Setup(r => r.DeleteAsync("area1")).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync("area1");

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync("area1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ChamaRepositoryDelete()
    {
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(MakeModel());
        _repoMock.Setup(r => r.DeleteAsync("area1")).Returns(Task.CompletedTask);

        await _service.DeleteAsync("area1");

        _repoMock.Verify(r => r.DeleteAsync("area1"), Times.Once);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_CommonAreaEncontrada_RetornaCommonArea()
    {
        var commonArea = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("area1")).ReturnsAsync(commonArea);

        var result = await _service.GetByIdAsync("area1");

        Assert.NotNull(result);
        Assert.Equal(commonArea.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_CommonAreaNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((CommonArea?)null);

        var result = await _service.GetByIdAsync("id-invalido");

        Assert.Null(result);
    }

    // ── GetAllAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_RetornaTodasAsAreas()
    {
        var areas = new List<CommonAreaModel> { MakeModel(), MakeModel() };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(areas);

        var result = await _service.GetAllAsync();

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_SemAreas_RetornaListaVazia()
    {
        var areas = new List<CommonAreaModel>();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(areas);

        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // ── GetByCondominiumAsync ────────────────────────────────────

    [Fact]
    public async Task GetByCondominiumAsync_RetornaAreasPorCondominio()
    {
        var areas = new List<CommonAreaModel> { MakeModel(), MakeModel() };
        _repoMock.Setup(r => r.GetByCondominiumAsync("cond1")).ReturnsAsync(areas);

        var result = await _service.GetByCondominiumAsync("cond1");

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByCondominiumAsync_CondominioSemAreas_RetornaListaVazia()
    {
        var areas = new List<CommonAreaModel>();
        _repoMock.Setup(r => r.GetByCondominiumAsync("cond1")).ReturnsAsync(areas);

        var result = await _service.GetByCondominiumAsync("cond1");

        Assert.Empty(result);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static CreateCommonAreaDto MakeDto() => new()
    {
        CondominiumId = "cond1",
        Name = "Piscina",
        Description = "Piscina da comunidade",
        Capacity = 50,
        OpenTime = "08:00",
        CloseTime = "22:00",
        Rules = "Uso de chinelo obrigatório"
    };

    private static CommonAreaModel MakeModel() => new()
    {
        Id = "area1",
        CondominiumId = "cond1",
        Name = "Piscina",
        Description = "Piscina da comunidade",
        Capacity = 50,
        OpenTime = "08:00",
        CloseTime = "22:00",
        Rules = "Uso de chinelo obrigatório",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}



