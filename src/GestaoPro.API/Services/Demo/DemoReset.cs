namespace GestaoPro.API.Services.Demo;

// Volta a demonstração ao estado inicial: apaga o condomínio demo, recria tudo e troca a época
// (tokens emitidos antes deixam de valer, porque apontam para ids que não existem mais).
public interface IDemoReset
{
    Task ResetAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoResetRunner : IDemoReset
{
    private readonly DemoDataCleaner _cleaner;
    private readonly DataSeeder _seeder;
    private readonly DemoCatalog _catalog;

    public DemoResetRunner(DemoDataCleaner cleaner, DataSeeder seeder, DemoCatalog catalog)
    {
        _cleaner = cleaner;
        _seeder = seeder;
        _catalog = catalog;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (!_catalog.Enabled) return;
        await _cleaner.ResetAsync(cancellationToken);
        await _seeder.SeedAsync();
        _catalog.RenewEpoch();
    }
}
