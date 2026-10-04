namespace GestaoPro.API.Services.Demo;

// Modo demonstração: de tempos em tempos (Demo:IntervaloResetHoras, padrão 6 h) devolve as contas demo
// e o condomínio de demonstração ao estado inicial (senhas, papéis, contas ativas, dados de exemplo).
// Desligado quando Demo:Enabled=false. A primeira execução é a da partida (Program.cs).
public sealed class DemoResetService : BackgroundService
{
    private readonly DemoCatalog _catalog;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DemoResetService> _logger;
    private readonly TimeSpan _interval;

    public DemoResetService(DemoCatalog catalog, IServiceScopeFactory scopes, IConfiguration configuration, ILogger<DemoResetService> logger)
    {
        _catalog = catalog;
        _scopes = scopes;
        _logger = logger;
        _interval = TimeSpan.FromHours(Math.Max(1, configuration.GetValue("Demo:IntervaloResetHoras", 6)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_catalog.Enabled) return;
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
                _logger.LogInformation("Modo demonstração restaurado.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao restaurar o modo demonstração.");
            }
        }
    }
}
