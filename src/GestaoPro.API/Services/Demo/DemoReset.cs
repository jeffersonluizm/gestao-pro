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

// Portão das escritas durante o reset da demonstração: conta as escritas em andamento e, enquanto o reset roda,
// recusa novas (o DemoResetGateMiddleware responde 503 com Retry-After).
public sealed class DemoResetGate
{
    private readonly object _lock = new();
    private int _inFlight;
    private bool _resetting;

    public bool Resetting { get { lock (_lock) return _resetting; } }

    public bool TryEnterWrite()
    {
        lock (_lock)
        {
            if (_resetting) return false;
            _inFlight++;
            return true;
        }
    }

    public void ExitWrite()
    {
        lock (_lock) _inFlight = Math.Max(0, _inFlight - 1);
    }

    public async Task BeginAsync(TimeSpan drainTimeout, CancellationToken cancellationToken = default)
    {
        lock (_lock) _resetting = true;
        var deadline = DateTime.UtcNow + drainTimeout;
        while (DateTime.UtcNow < deadline)
        {
            lock (_lock) if (_inFlight == 0) return;
            await Task.Delay(25, cancellationToken);
        }
    }

    public void End()
    {
        lock (_lock) _resetting = false;
    }
}
