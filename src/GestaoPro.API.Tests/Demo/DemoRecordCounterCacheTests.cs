using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Tests.Demo;

// A contagem do teto por coleção ia ao banco a cada criação: no teste de estresse, H5 (publicar aviso) caiu de
// cerca de 7.500 para 130 req/s. A contagem passa a ficar guardada por alguns segundos por coleção e condomínio.
public class DemoRecordCounterCacheTests
{
    private sealed class RelogioManual : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private readonly Mock<IDemoRecordCounter> _inner = new();
    private readonly RelogioManual _relogio = new();

    private CachedDemoRecordCounter Counter() => new(_inner.Object, _relogio, TimeSpan.FromSeconds(5));

    [Fact]
    public async Task Dentro_da_validade_usa_a_contagem_guardada()
    {
        _inner.Setup(c => c.CountAsync("announcements", "c1", true, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        var counter = Counter();

        var first = await counter.CountAsync("announcements", "c1", true, CancellationToken.None);
        _relogio.Agora = _relogio.Agora.AddSeconds(4);
        var second = await counter.CountAsync("announcements", "c1", true, CancellationToken.None);

        Assert.Equal(10, first);
        Assert.Equal(10, second);
        _inner.Verify(c => c.CountAsync("announcements", "c1", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Depois_da_validade_conta_de_novo()
    {
        _inner.SetupSequence(c => c.CountAsync("announcements", "c1", true, It.IsAny<CancellationToken>())).ReturnsAsync(10).ReturnsAsync(12);
        var counter = Counter();

        await counter.CountAsync("announcements", "c1", true, CancellationToken.None);
        _relogio.Agora = _relogio.Agora.AddSeconds(6);

        Assert.Equal(12, await counter.CountAsync("announcements", "c1", true, CancellationToken.None));
    }

    [Fact]
    public async Task Colecoes_diferentes_tem_contagens_separadas()
    {
        _inner.Setup(c => c.CountAsync("announcements", "c1", true, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        _inner.Setup(c => c.CountAsync("units", "c1", true, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        var counter = Counter();

        Assert.Equal(10, await counter.CountAsync("announcements", "c1", true, CancellationToken.None));
        Assert.Equal(3, await counter.CountAsync("units", "c1", true, CancellationToken.None));
    }
}
