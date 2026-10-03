namespace GestaoPro.API.Tests.Announcement;

using AnnouncementModel = GestaoPro.API.Models.Announcement;

public class AnnouncementServiceTests
{
    private readonly Mock<IAnnouncementRepository> _repoMock = new();
    private readonly AnnouncementService _service;

    public AnnouncementServiceTests()
    {
        _service = new AnnouncementService(_repoMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaAnnouncement()
    {
        var dto = MakeDto();
        var announcementRetornado = MakeModel();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AnnouncementModel>()))
                 .ReturnsAsync(announcementRetornado);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.CondominiumId, result.CondominiumId);
        Assert.Equal(dto.AuthorId, result.AuthorId);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Content, result.Content);
        Assert.Equal(dto.Category, result.Category);
    }

    [Fact]
    public async Task CreateAsync_StatusAtivoParPadrao()
    {
        var dto = MakeDto();
        var announcementRetornado = MakeModel();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AnnouncementModel>()))
                 .ReturnsAsync(announcementRetornado);

        var result = await _service.CreateAsync(dto);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ComDataExpiracao()
    {
        var dto = MakeDto();
        var dataExpiracao = DateTime.Today.AddDays(30);
        dto.ExpiresAt = dataExpiracao;
        var announcementRetornado = MakeModel();
        announcementRetornado.ExpiresAt = dataExpiracao;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AnnouncementModel>()))
                 .ReturnsAsync(announcementRetornado);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(dataExpiracao, result.ExpiresAt);
    }

    [Fact]
    public async Task CreateAsync_CriaComTimestamps()
    {
        var dto = MakeDto();
        var beforeCall = DateTime.UtcNow;
        var announcementRetornado = MakeModel();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AnnouncementModel>()))
                 .ReturnsAsync(announcementRetornado);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result.CreatedAt);
        Assert.NotNull(result.UpdatedAt);
    }

    // ── UpdateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AnnouncementNaoEncontrado_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((AnnouncementModel?)null);

        var result = await _service.UpdateAsync("id-invalido", new UpdateAnnouncementDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaTitulo_RetornaAnnouncementAtualizado()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { Title = "Novo Título" });

        Assert.NotNull(result);
        Assert.Equal("Novo Título", result!.Title);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaConteudo_RetornaAnnouncementAtualizado()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { Content = "Novo conteúdo" });

        Assert.NotNull(result);
        Assert.Equal("Novo conteúdo", result!.Content);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaCategoria_RetornaAnnouncementAtualizado()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { Category = "Urgente" });

        Assert.NotNull(result);
        Assert.Equal("Urgente", result!.Category);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaStatus_RetornaAnnouncementAtualizado()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { IsActive = false });

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaDataExpiracao_RetornaAnnouncementAtualizado()
    {
        var existing = MakeModel();
        var novaDataExpiracao = DateTime.Today.AddDays(15);
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { ExpiresAt = novaDataExpiracao });

        Assert.NotNull(result);
        Assert.Equal(novaDataExpiracao, result!.ExpiresAt);
    }

    [Fact]
    public async Task UpdateAsync_ValoresNulos_NaoAlteraAtributos()
    {
        var existing = MakeModel();
        var originalTitle = existing.Title;
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto());

        Assert.NotNull(result);
        Assert.Equal(originalTitle, result!.Title);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaTimestampUpdatedAt()
    {
        var existing = MakeModel();
        var beforeCall = DateTime.UtcNow;
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { Title = "Novo Título" });

        Assert.NotNull(result);
        Assert.InRange(result!.UpdatedAt, beforeCall, DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_ChamaRepositoryUpdate()
    {
        var existing = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()))
                 .Returns(Task.CompletedTask);

        await _service.UpdateAsync("ann1", new UpdateAnnouncementDto { Title = "Novo Título" });

        _repoMock.Verify(r => r.UpdateAsync("ann1", It.IsAny<AnnouncementModel>()), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_AnnouncementNaoEncontrado_RetornaFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((AnnouncementModel?)null);

        var result = await _service.DeleteAsync("id-invalido");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_AnnouncementEncontrado_RetornaTrue()
    {
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(MakeModel());
        _repoMock.Setup(r => r.DeleteAsync("ann1")).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync("ann1");

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync("ann1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ChamaRepositoryDelete()
    {
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(MakeModel());
        _repoMock.Setup(r => r.DeleteAsync("ann1")).Returns(Task.CompletedTask);

        await _service.DeleteAsync("ann1");

        _repoMock.Verify(r => r.DeleteAsync("ann1"), Times.Once);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_AnnouncementEncontrado_RetornaAnnouncement()
    {
        var announcement = MakeModel();
        _repoMock.Setup(r => r.GetByIdAsync("ann1")).ReturnsAsync(announcement);

        var result = await _service.GetByIdAsync("ann1");

        Assert.NotNull(result);
        Assert.Equal(announcement.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AnnouncementNaoEncontrado_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((AnnouncementModel?)null);

        var result = await _service.GetByIdAsync("id-invalido");

        Assert.Null(result);
    }

    // ── GetAllAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_RetornaTodosComunicados()
    {
        var announcements = new List<AnnouncementModel> { MakeModel(), MakeModel() };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(announcements);

        var result = await _service.GetAllAsync();

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_SemComunicados_RetornaListaVazia()
    {
        var announcements = new List<AnnouncementModel>();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(announcements);

        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // ── GetByCondominiumAsync ────────────────────────────────────

    [Fact]
    public async Task GetByCondominiumAsync_RetornaComunicadosPorCondominio()
    {
        var announcements = new List<AnnouncementModel> { MakeModel(), MakeModel() };
        _repoMock.Setup(r => r.GetByCondominiumAsync("cond1")).ReturnsAsync(announcements);

        var result = await _service.GetByCondominiumAsync("cond1");

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByCondominiumAsync_CondominioSemComunicados_RetornaListaVazia()
    {
        var announcements = new List<AnnouncementModel>();
        _repoMock.Setup(r => r.GetByCondominiumAsync("cond1")).ReturnsAsync(announcements);

        var result = await _service.GetByCondominiumAsync("cond1");

        Assert.Empty(result);
    }

    // ── GetActiveAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetActiveAsync_RetornaComunicadosAtivos()
    {
        var announcements = new List<AnnouncementModel> { MakeModel() };
        _repoMock.Setup(r => r.GetActiveAsync()).ReturnsAsync(announcements);

        var result = await _service.GetActiveAsync();

        Assert.NotEmpty(result);
    }

    // ── GetByCategoryAsync ───────────────────────────────────────

    [Fact]
    public async Task GetByCategoryAsync_RetornaComunicadosPorCategoria()
    {
        var announcements = new List<AnnouncementModel> { MakeModel() };
        _repoMock.Setup(r => r.GetByCategoryAsync("Importante")).ReturnsAsync(announcements);

        var result = await _service.GetByCategoryAsync("Importante");

        Assert.NotEmpty(result);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static CreateAnnouncementDto MakeDto() => new()
    {
        CondominiumId = "cond1",
        AuthorId = "admin1",
        Title = "Aviso Importante",
        Content = "Conteúdo do comunicado",
        Category = "Importante",
        ExpiresAt = DateTime.Today.AddDays(30)
    };

    private static AnnouncementModel MakeModel() => new()
    {
        Id = "ann1",
        CondominiumId = "cond1",
        AuthorId = "admin1",
        Title = "Aviso Importante",
        Content = "Conteúdo do comunicado",
        Category = "Importante",
        IsActive = true,
        ExpiresAt = DateTime.Today.AddDays(30),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}

