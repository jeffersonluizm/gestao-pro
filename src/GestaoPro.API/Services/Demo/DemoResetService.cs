namespace GestaoPro.API.Services.Demo;

// Modo demonstração: de tempos em tempos (Demo:IntervaloResetMinutos, padrão 90, entre 15 e 1440) apaga o que
// os visitantes criaram no condomínio de demonstração e recria contas e dados de exemplo do zero.
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
        _interval = Interval(configuration);
    }

    public static TimeSpan Interval(IConfiguration configuration) =>
        TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Demo:IntervaloResetMinutos", 90), 15, 1440));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_catalog.Enabled) return;
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IDemoReset>().ResetAsync(stoppingToken);
                _logger.LogInformation("Modo demonstração restaurado.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao restaurar o modo demonstração.");
            }
        }
    }
}
