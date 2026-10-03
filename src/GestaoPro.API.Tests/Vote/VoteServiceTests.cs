namespace GestaoPro.API.Tests.Vote;

public class VoteServiceTests
{
    private readonly Mock<IVoteRepository>      _voteRepoMock    = new();
    private readonly Mock<IAssemblyRepository>  _assemblyMock    = new();
    private readonly Mock<IProxyVoteRepository> _proxyMock       = new();
    private readonly Mock<IUserRepository>      _userRepoMock    = new();
    private readonly VoteService                _service;
    private readonly DateTime                   _now = new(2026, 6, 8, 15, 0, 0, DateTimeKind.Utc);

    public VoteServiceTests()
    {
        _service = new VoteService(
            _voteRepoMock.Object,
            _assemblyMock.Object,
            _proxyMock.Object,
            _userRepoMock.Object,
            new FixedTimeProvider(_now)
        );
    }

    // ── RegisterVoteAsync: assembly inválida ─────────────────────

    [Fact]
    public async Task RegisterVoteAsync_AssembleiaNaoEncontrada_LancaExcecao404()
    {
        _assemblyMock.Setup(r => r.GetByIdAsync("sem-id")).ReturnsAsync((AssemblyModel?)null);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("sem-id", "user1", "sim"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task RegisterVoteAsync_AssembleiaSemTopicos_LancaExcecao400()
    {
        var assembly = MakeAssembly(topics: new List<TopicModel>());
        _assemblyMock.Setup(r => r.GetByIdAsync("a1")).ReturnsAsync(assembly);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "sim"));

        Assert.Equal(400, ex.StatusCode);
    }

    // ── RegisterVoteAsync: usuário inválido ───────────────────────

    [Fact]
    public async Task RegisterVoteAsync_UsuarioNaoEncontrado_LancaExcecao404()
    {
        _assemblyMock.Setup(r => r.GetByIdAsync("a1")).ReturnsAsync(MakeAssembly());
        _userRepoMock.Setup(r => r.GetByIdAsync("user-x", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user-x", "sim"));

        Assert.Equal(404, ex.StatusCode);
    }

    // ── RegisterVoteAsync: validações de data ────────────────────

    [Fact]
    public async Task RegisterVoteAsync_AssembleiaNaoIniciada_LancaExcecao400()
    {
        var assembly = MakeAssembly(start: _now.AddSeconds(1));
        SetupUserAndAssembly("a1", assembly);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "sim"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("A votação ainda não foi iniciada.", ex.Message);
    }

    [Fact]
    public async Task RegisterVoteAsync_AssembleiaEncerrada_LancaExcecao400()
    {
        var assembly = MakeAssembly(end: _now.AddSeconds(-1));
        SetupUserAndAssembly("a1", assembly);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "sim"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("A votação já foi encerrada.", ex.Message);
    }

    [Fact]
    public async Task RegisterVoteAsync_ExatamenteNoInicio_PermiteVoto()
    {
        var assembly = MakeAssembly(start: _now, end: _now.AddHours(1));
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task RegisterVoteAsync_DurantePeriodo_PermiteVoto()
    {
        var assembly = MakeAssembly(start: _now.AddMinutes(-1), end: _now.AddMinutes(1));
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task RegisterVoteAsync_ExatamenteNoFim_PermiteVoto()
    {
        var assembly = MakeAssembly(start: _now.AddHours(-1), end: _now);
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim");

        Assert.NotNull(result);
    }

    // ── RegisterVoteAsync: status da assembleia ──────────────────

    [Fact]
    public async Task RegisterVoteAsync_StatusAgendadaDentroDoPeriodo_PermiteVoto()
    {
        var assembly = MakeAssembly(status: "agendada");
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim");

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("encerrada")]
    [InlineData("cancelada")]
    public async Task RegisterVoteAsync_StatusBloqueadoDentroDoPeriodo_LancaExcecao400(string status)
    {
        var assembly = MakeAssembly(status: status);
        SetupUserAndAssembly("a1", assembly);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "sim"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("não está ativa", ex.Message);
    }

    [Fact]
    public async Task RegisterVoteAsync_StatusComTypo_EnAndamento_EhNormalizado()
    {
        // "en_andamento" deve ser normalizado para "em_andamento"
        var assembly = MakeAssembly(status: "en_andamento");
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim");

        Assert.NotNull(result);
    }

    // ── RegisterVoteAsync: validação do valor do voto ─────────────

    [Fact]
    public async Task RegisterVoteAsync_VotoInvalido_LancaExcecao400()
    {
        var assembly = MakeAssembly();
        SetupUserAndAssembly("a1", assembly);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "talvez"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Voto inválido", ex.Message);
    }

    [Theory]
    [InlineData("sim")]
    [InlineData("SIM")]
    [InlineData("não")]
    [InlineData("NÃO")]
    public async Task RegisterVoteAsync_VotosValidos_NaoLancaExcecaoDeVotoInvalido(string voto)
    {
        var assembly = MakeAssembly();
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        // Não deve lançar exceção de voto inválido
        var result = await _service.RegisterVoteAsync("a1", "user1", voto);
        Assert.NotNull(result);
    }

    // ── RegisterVoteAsync: duplicidade de voto ────────────────────

    [Fact]
    public async Task RegisterVoteAsync_VotoCustomizadoCadastrado_RegistraOpcao()
    {
        var assembly = MakeAssembly(topics: new List<TopicModel>
        {
            new() { Title = "Escolha do conselho", VoteOptions = new List<string> { "Chapa A", "Chapa B" } }
        });
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "Chapa B");

        Assert.Equal("Chapa B", result.Vote);
        Assert.Equal(0, result.TopicIndex);
    }

    [Fact]
    public async Task RegisterVoteAsync_VotoForaDasOpcoesCadastradas_LancaExcecao400()
    {
        var assembly = MakeAssembly(topics: new List<TopicModel>
        {
            new() { Title = "Escolha do conselho", VoteOptions = new List<string> { "Chapa A", "Chapa B" } }
        });
        SetupUserAndAssembly("a1", assembly);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "Chapa C"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("opções cadastradas", ex.Message);
    }

    [Fact]
    public async Task RegisterVoteAsync_UsuarioJaVotou_LancaExcecao409()
    {
        var assembly = MakeAssembly();
        SetupUserAndAssembly("a1", assembly);
        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync("a1", "user1", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "sim"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task RegisterVoteAsync_UsuarioVotouEmOutraPauta_PermiteVoto()
    {
        var assembly = MakeAssembly(topics: new List<TopicModel>
        {
            new() { Title = "Pauta 1" },
            new() { Title = "Pauta 2" }
        });
        SetupUserAndAssembly("a1", assembly);
        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync("a1", "user1", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _proxyMock
            .Setup(r => r.GetByAssemblyAndDelegatorAsync("a1", "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProxyVoteModel?)null);
        _proxyMock
            .Setup(r => r.GetByAssemblyAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProxyVoteModel>());
        _voteRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<VoteModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteModel vote, CancellationToken _) => vote);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim", 1);

        Assert.Equal(1, result.TopicIndex);
        _voteRepoMock.Verify(
            r => r.HasUserVotedAsync("a1", "user1", 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── RegisterVoteAsync: delegação (proxy) ──────────────────────

    [Fact]
    public async Task RegisterVoteAsync_UsuarioDelegouVoto_LancaExcecao403()
    {
        var assembly = MakeAssembly();
        SetupUserAndAssembly("a1", assembly);
        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync("a1", "user1", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _proxyMock
            .Setup(r => r.GetByAssemblyAndDelegatorAsync("a1", "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProxyVoteModel { AssemblyId = "a1", DelegatorUserId = "user1", DelegateUserId = "user2" });

        var ex = await Assert.ThrowsAsync<VoteOperationException>(
            () => _service.RegisterVoteAsync("a1", "user1", "sim"));

        Assert.Equal(403, ex.StatusCode);
    }

    // ── RegisterVoteAsync: fluxo feliz ────────────────────────────

    [Fact]
    public async Task RegisterVoteAsync_VotoValido_CriaERetornaVoto()
    {
        var assembly = MakeAssembly();
        SetupVoteFlow("a1", assembly, alreadyVoted: false, hasProxy: false, isDelegator: false);

        var result = await _service.RegisterVoteAsync("a1", "user1", "sim");

        Assert.NotNull(result);
        Assert.Equal("a1", result.AssemblyId);
        Assert.Equal("user1", result.UserId);
        Assert.Equal("sim", result.Vote);
        _voteRepoMock.Verify(r => r.CreateAsync(It.Is<VoteModel>(v => v.UserId == "user1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterVoteAsync_UsuarioEhRepresentante_RegistraVotosDelegados()
    {
        var assembly = MakeAssembly();
        SetupUserAndAssembly("a1", assembly);
        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync("a1", "user1", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _proxyMock
            .Setup(r => r.GetByAssemblyAndDelegatorAsync("a1", "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProxyVoteModel?)null);
        // user1 é representante de user2
        _proxyMock
            .Setup(r => r.GetByAssemblyAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProxyVoteModel>
            {
                new() { AssemblyId = "a1", DelegatorUserId = "user2", DelegateUserId = "user1" }
            });
        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync("a1", "user2", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _voteRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<VoteModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteModel v, CancellationToken _) => v);

        await _service.RegisterVoteAsync("a1", "user1", "sim");

        // Deve registrar voto em nome de user2 + voto próprio de user1
        _voteRepoMock.Verify(r => r.CreateAsync(
            It.Is<VoteModel>(v => v.UserId == "user2"), It.IsAny<CancellationToken>()), Times.Once);
        _voteRepoMock.Verify(r => r.CreateAsync(
            It.Is<VoteModel>(v => v.UserId == "user1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── HasUserVotedAsync ─────────────────────────────────────────

    [Fact]
    public async Task HasUserVotedAsync_DelegaParaRepositorio()
    {
        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync("a1", "user1", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.HasUserVotedAsync("a1", "user1", 1);

        Assert.True(result);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private AssemblyModel MakeAssembly(
        DateTime? start = null,
        DateTime? end = null,
        string status = "em_andamento",
        List<TopicModel>? topics = null) => new()
    {
        Id = "a1",
        CondominiumId = "cond1",
        Title = "Assembleia Teste",
        Description = "Desc",
        StartTime = start ?? _now.AddHours(-1),
        EndTime = end ?? _now.AddDays(7),
        Status = status,
        Topics = topics ?? new List<TopicModel> { new() { Title = "Pauta 1" } }
    };

    private void SetupUserAndAssembly(string assemblyId, AssemblyModel assembly)
    {
        _assemblyMock.Setup(r => r.GetByIdAsync(assemblyId)).ReturnsAsync(assembly);
        _userRepoMock.Setup(r => r.GetByIdAsync("user1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new User { Id = "user1", Name = "Morador Teste" });
    }

    private void SetupVoteFlow(string assemblyId, AssemblyModel assembly,
        bool alreadyVoted, bool hasProxy, bool isDelegator)
    {
        SetupUserAndAssembly(assemblyId, assembly);

        _voteRepoMock
            .Setup(r => r.HasUserVotedAsync(assemblyId, "user1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyVoted);

        _proxyMock
            .Setup(r => r.GetByAssemblyAndDelegatorAsync(assemblyId, "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasProxy ? new ProxyVoteModel { DelegatorUserId = "user1" } : null);

        _proxyMock
            .Setup(r => r.GetByAssemblyAsync(assemblyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isDelegator
                ? new List<ProxyVoteModel> { new() { DelegateUserId = "user1", DelegatorUserId = "user2" } }
                : new List<ProxyVoteModel>());

        _voteRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<VoteModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteModel v, CancellationToken _) => v);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
