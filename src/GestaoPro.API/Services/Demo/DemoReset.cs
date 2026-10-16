namespace GestaoPro.API.Services.Demo;

// Volta a demonstração ao estado inicial: apaga o condomínio demo, recria tudo e troca a época
// (tokens emitidos antes deixam de valer, porque apontam para ids que não existem mais).
public interface IDemoReset
{
    Task ResetAsync(CancellationToken cancellationToken = default);
}

public interface IDemoCleaner
{
    Task ResetAsync(CancellationToken cancellationToken = default);
}

public interface IDemoSeeder
{
    Task SeedAsync();
}

// Ordem do reset: 1) fecha as escritas e espera as que estão em andamento terminarem; 2) troca a época (nenhum token
// antigo vale durante a limpeza); 3) limpa e semeia. Um conflito ao semear (dado gravado na janela) não aborta o reset:
// limpa e semeia de novo. As escritas reabrem no fim, mesmo em erro.
public sealed class DemoResetRunner : IDemoReset
{
    public static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(10);
    private const int Attempts = 3;

    private readonly IDemoCleaner _cleaner;
    private readonly IDemoSeeder _seeder;
    private readonly DemoCatalog _catalog;
    private readonly DemoResetGate _gate;

    public DemoResetRunner(IDemoCleaner cleaner, IDemoSeeder seeder, DemoCatalog catalog, DemoResetGate gate)
    {
        _cleaner = cleaner;
        _seeder = seeder;
        _catalog = catalog;
        _gate = gate;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (!_catalog.Enabled) return;
        await _gate.BeginAsync(DrainTimeout, cancellationToken);
        try
        {
            _catalog.RenewEpoch();
            for (var attempt = 1; ; attempt++)
            {
                await _cleaner.ResetAsync(cancellationToken);
                try
                {
                    await _seeder.SeedAsync();
                    return;
                }
                catch (Exception) when (attempt < Attempts)
                {
                    // Conflito com algo gravado na janela: a próxima limpeza remove e o seed recomeça.
                }
            }
        }
        finally
        {
            _gate.End();
        }
    }
}

// Portão das escritas durante o reset da demonstração: guarda as escritas em andamento e, enquanto o reset roda,
// recusa novas (o DemoResetGateMiddleware responde 503 com Retry-After). Se a espera se esgotar (escrita lenta, por
// exemplo um corpo chunked a poucos bytes por segundo), aborta as que sobraram e registra aviso antes de limpar.
public sealed class DemoResetGate
{
    private readonly object _lock = new();
    private readonly HashSet<HttpContext> _inFlight = new();
    private readonly ILogger<DemoResetGate>? _logger;
    private bool _resetting;

    public DemoResetGate(ILogger<DemoResetGate>? logger = null) => _logger = logger;

    public bool Resetting { get { lock (_lock) return _resetting; } }

    public bool TryEnterWrite(HttpContext context)
    {
        lock (_lock)
        {
            if (_resetting) return false;
            _inFlight.Add(context);
            return true;
        }
    }

    public void ExitWrite(HttpContext context)
    {
        lock (_lock) _inFlight.Remove(context);
    }

    public async Task BeginAsync(TimeSpan drainTimeout, CancellationToken cancellationToken = default)
    {
        lock (_lock) _resetting = true;
        var deadline = DateTime.UtcNow + drainTimeout;
        while (DateTime.UtcNow < deadline)
        {
            lock (_lock) if (_inFlight.Count == 0) return;
            await Task.Delay(25, cancellationToken);
        }

        HttpContext[] pending;
        lock (_lock) pending = _inFlight.ToArray();
        foreach (var context in pending)
        {
            _logger?.LogWarning("Reset da demonstração: escrita {Method} {Path} ainda em andamento depois de {Seconds} s foi abortada.",
                context.Request.Method, context.Request.Path, drainTimeout.TotalSeconds);
            context.Abort();
        }
    }

    public void End()
    {
        lock (_lock) _resetting = false;
    }
}
