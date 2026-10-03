namespace GestaoPro.API.Tests.Assembly;

public class AssemblyServiceTests
{
    private readonly Mock<IAssemblyRepository> _repoMock = new();
    private readonly AssemblyService _service;

    public AssemblyServiceTests()
    {
        _service = new AssemblyService(_repoMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DataFutura_RetornaAssembly()
    {
        var dto = MakeDto(DateTime.Today.AddDays(1));
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel a) => { a.Id = "abc123"; return a; });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal("agendada", result.Status);
    }

    [Fact]
    public async Task CreateAsync_DataPassada_LancaInvalidOperationException()
    {
        var dto = MakeDto(DateTime.Today.AddDays(-1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_InicioHojeJaPassouMasFimAindaNao_PermiteCriacao()
    {
        var dto = MakeDto(DateTime.UtcNow.Date);
        dto.StartDate = DateTime.UtcNow.AddHours(-1);
        dto.EndDate = DateTime.UtcNow.AddHours(1);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel assembly) => assembly);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(dto.StartDate, result.StartTime);
        Assert.Equal(dto.EndDate, result.EndTime);
    }

    [Fact]
    public async Task CreateAsync_PeriodoDeHojeJaEncerrado_LancaInvalidOperationException()
    {
        var dto = MakeDto(DateTime.UtcNow.Date);
        dto.StartDate = DateTime.UtcNow.AddHours(-2);
        dto.EndDate = DateTime.UtcNow.AddHours(-1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));

        Assert.Contains("já foi encerrado", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_DataFinalMenorQueInicial_LancaInvalidOperationException()
    {
        var dto = MakeDto(DateTime.Today.AddDays(5));
        dto.EndDate = DateTime.Today.AddDays(3); // end < start

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_PeriodoComHorario_PreservaHorarioEmUtc()
    {
        var start = DateTime.UtcNow.Date.AddDays(2).AddHours(13).AddMinutes(30);
        var end = start.AddHours(5).AddMinutes(15);
        var dto = MakeDto(start);
        dto.StartDate = start;
        dto.EndDate = end;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(start, result.StartTime);
        Assert.Equal(end, result.EndTime);
        Assert.Equal(DateTimeKind.Utc, result.StartTime.Kind);
        Assert.Equal(DateTimeKind.Utc, result.EndTime.Kind);
    }

    [Fact]
    public async Task CreateAsync_TopicsPorTexto_NormalizaTitulosCorretamente()
    {
        var dto = MakeDto(DateTime.Today.AddDays(1));
        dto.TopicsText = "Aprovação do orçamento\nReforma da piscina";
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(2, result.Topics.Count);
        Assert.Equal("Aprovação do orçamento", result.Topics[0].Title);
        Assert.Equal("Reforma da piscina", result.Topics[1].Title);
    }

    [Fact]
    public async Task CreateAsync_TopicsPorTexto_CadaTopicTemOpcoesPadrao()
    {
        var dto = MakeDto(DateTime.Today.AddDays(1));
        dto.TopicsText = "Pauta única";
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.Contains("sim", result.Topics[0].VoteOptions);
        Assert.Contains("não", result.Topics[0].VoteOptions);
    }

    [Fact]
    public async Task CreateAsync_TopicsPorLista_UsaListaForecida()
    {
        var dto = MakeDto(DateTime.Today.AddDays(1));
        dto.Topics = new List<CreateTopicDto>
        {
            new() { Title = "Pauta A", Desc = "Desc A", VoteOptions = new() { "sim", "não", "abstenção" } }
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.Single(result.Topics);
        Assert.Equal("Pauta A", result.Topics[0].Title);
        Assert.Equal(3, result.Topics[0].VoteOptions.Count);
    }

    [Fact]
    public async Task CreateAsync_SemTopicos_CriaListaVazia()
    {
        var dto = MakeDto(DateTime.Today.AddDays(1));
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AssemblyModel>()))
                 .ReturnsAsync((AssemblyModel a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.Empty(result.Topics);
    }

    // ── UpdateAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AssemblyNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((AssemblyModel?)null);

        var result = await _service.UpdateAsync("id-invalido", new UpdateAssemblyDto { Title = "Novo" });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaTitulo_RetornaAssemblyAtualizada()
    {
        var existing = MakeAssembly();
        _repoMock.Setup(r => r.GetByIdAsync("id1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("id1", It.IsAny<AssemblyModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("id1", new UpdateAssemblyDto { Title = "Título Novo" });

        Assert.NotNull(result);
        Assert.Equal("Título Novo", result!.Title);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaStatus_RetornaAssemblyAtualizada()
    {
        var existing = MakeAssembly();
        _repoMock.Setup(r => r.GetByIdAsync("id1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("id1", It.IsAny<AssemblyModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("id1", new UpdateAssemblyDto { Status = "em_andamento" });

        Assert.Equal("em_andamento", result!.Status);
    }

    // ── DeleteAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_AssemblyNaoEncontrada_RetornaFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((AssemblyModel?)null);

        var result = await _service.DeleteAsync("id-invalido");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_AssemblyEncontrada_RetornaTrue()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id1")).ReturnsAsync(MakeAssembly());
        _repoMock.Setup(r => r.DeleteAsync("id1")).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync("id1");

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync("id1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ComUsuario_ExecutaSoftDelete()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id1")).ReturnsAsync(MakeAssembly());

        var result = await _service.DeleteAsync("id1", "user1");

        Assert.True(result);
        _repoMock.Verify(r => r.SoftDeleteAsync("id1", "user1"), Times.Once);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AssemblyJaRemovida_RetornaFalse()
    {
        var assembly = MakeAssembly();
        assembly.IsDeleted = true;
        _repoMock.Setup(r => r.GetByIdAsync("id1")).ReturnsAsync(assembly);

        var result = await _service.DeleteAsync("id1", "user1");

        Assert.False(result);
        _repoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static CreateAssemblyDto MakeDto(DateTime scheduledDate) => new()
    {
        CondominiumId = "cond1",
        Title = "Assembleia de Teste",
        Description = "Descrição",
        ScheduledDate = scheduledDate,
    };

    private static AssemblyModel MakeAssembly() => new()
    {
        Id = "id1",
        CondominiumId = "cond1",
        Title = "Assembleia Existente",
        Description = "Desc",
        StartTime = DateTime.Today.AddDays(1),
        EndTime = DateTime.Today.AddDays(2),
        Status = "agendada",
        Topics = new List<TopicModel> { new() { Title = "Pauta 1" } }
    };
}
