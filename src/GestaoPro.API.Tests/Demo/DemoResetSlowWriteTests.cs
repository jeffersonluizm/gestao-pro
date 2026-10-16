using System.Security.Claims;
using GestaoPro.API.Services.Demo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Demo;

// Uma escrita lenta (corpo chunked a poucos bytes por segundo) atravessava o reset: a espera de 10 s se esgotava em
// silêncio e o dado aparecia no estado limpo. Agora a espera esgotada aborta as escritas pendentes e registra aviso, e
// uma escrita que chegue à ação depois da troca de época é recusada.
public class DemoResetSlowWriteTests
{
    private sealed class LifetimeSpy : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; }
        public bool Aborted { get; private set; }
        public void Abort() => Aborted = true;
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private static (DefaultHttpContext Context, LifetimeSpy Lifetime) SlowWrite()
    {
        var context = new DefaultHttpContext();
        var lifetime = new LifetimeSpy();
        context.Features.Set<IHttpRequestLifetimeFeature>(lifetime);
        return (context, lifetime);
    }

    [Fact]
    public async Task Espera_esgotada_aborta_as_escritas_pendentes_e_registra_aviso()
    {
        var logger = new ListLogger<DemoResetGate>();
        var gate = new DemoResetGate(logger);
        var (context, lifetime) = SlowWrite();
        Assert.True(gate.TryEnterWrite(context));

        await gate.BeginAsync(TimeSpan.FromMilliseconds(100));

        Assert.True(lifetime.Aborted);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("abortada"));
    }

    [Fact]
    public async Task Escrita_que_termina_a_tempo_nao_e_abortada()
    {
        var gate = new DemoResetGate(new ListLogger<DemoResetGate>());
        var (context, lifetime) = SlowWrite();
        Assert.True(gate.TryEnterWrite(context));
        _ = Task.Run(async () => { await Task.Delay(50); gate.ExitWrite(context); });

        await gate.BeginAsync(TimeSpan.FromSeconds(2));

        Assert.False(lifetime.Aborted);
    }

    private static DemoCatalog Catalog()
    {
        var catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build());
        catalog.CondominiumId = CondominioA;
        return catalog;
    }

    private static async Task<(IActionResult? Result, bool NextCalled)> RunEpochFilterAsync(DemoCatalog catalog, DemoResetGate gate, string method, string? epoch)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u1") };
        if (epoch is not null) claims.Add(new Claim(DemoCatalog.EpochClaim, epoch));
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        var context = new ActionExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
        var nextCalled = false;
        await new DemoEpochWriteFilter(catalog, gate).OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        });
        return (context.Result, nextCalled);
    }

    // O corpo lento termina de chegar depois do reset: a ação (que grava) não roda com a época antiga.
    [Fact]
    public async Task Escrita_com_epoca_antiga_e_recusada_antes_de_gravar()
    {
        var catalog = Catalog();
        var oldEpoch = catalog.Epoch;
        catalog.RenewEpoch();

        var (result, nextCalled) = await RunEpochFilterAsync(catalog, new DemoResetGate(), "POST", oldEpoch);

        Assert.False(nextCalled);
        Assert.Equal(401, StatusOf(result!));
    }

    [Fact]
    public async Task Escrita_durante_o_reset_e_recusada_com_503()
    {
        var catalog = Catalog();
        var gate = new DemoResetGate();
        await gate.BeginAsync(TimeSpan.FromMilliseconds(10));

        var (result, nextCalled) = await RunEpochFilterAsync(catalog, gate, "PUT", catalog.Epoch);

        Assert.False(nextCalled);
        Assert.Equal(503, StatusOf(result!));
    }

    [Fact]
    public async Task Escrita_com_a_epoca_atual_segue()
    {
        var catalog = Catalog();

        var (_, nextCalled) = await RunEpochFilterAsync(catalog, new DemoResetGate(), "POST", catalog.Epoch);

        Assert.True(nextCalled);
    }
}

public class DemoQuotaParallelTests
{
    private static ActionExecutingContext Context() => new(
        new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
        new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());

    private static (DemoCatalog Catalog, IConfiguration Config) Setup(int limit)
    {
        var catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build());
        catalog.CondominiumId = CondominioA;
        return (catalog, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:LimitePorColecao"] = limit.ToString() }).Build());
    }

    private static async Task<IActionResult?> CreateAsync(DemoCatalog catalog, IConfiguration config, CachedDemoRecordCounter counter, IActionResult outcome)
    {
        var filter = new DemoQuotaFilter(catalog, counter, config, "announcements", true);
        var context = Context();
        await filter.OnActionExecutionAsync(context, async () =>
        {
            await Task.Delay(10);
            return new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()) { Result = outcome };
        });
        return context.Result;
    }

    // 20 criações simultâneas com teto 8: a vaga é reservada antes da ação, então passam exatamente 8.
    [Fact]
    public async Task Rajada_paralela_nao_passa_do_teto()
    {
        var inner = new Mock<IDemoRecordCounter>();
        inner.Setup(c => c.CountAsync("announcements", CondominioA, true, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var counter = new CachedDemoRecordCounter(inner.Object, TimeProvider.System, TimeSpan.FromSeconds(5));
        var (catalog, config) = Setup(8);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => CreateAsync(catalog, config, counter, new CreatedResult("", null))));

        Assert.Equal(8, results.Count(result => result is null));
        Assert.Equal(12, results.Count(result => result is ConflictObjectResult));
    }

    // Criação que falha (400) devolve a vaga.
    [Fact]
    public async Task Criacao_que_falha_devolve_a_vaga()
    {
        var inner = new Mock<IDemoRecordCounter>();
        inner.Setup(c => c.CountAsync("announcements", CondominioA, true, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var counter = new CachedDemoRecordCounter(inner.Object, TimeProvider.System, TimeSpan.FromSeconds(5));
        var (catalog, config) = Setup(8);

        var failed = await CreateAsync(catalog, config, counter, new BadRequestObjectResult(new { message = "inválido" }));
        var next = await CreateAsync(catalog, config, counter, new CreatedResult("", null));
        var full = await CreateAsync(catalog, config, counter, new CreatedResult("", null));

        Assert.Null(failed);
        Assert.Null(next);
        Assert.IsType<ConflictObjectResult>(full);
    }
}
