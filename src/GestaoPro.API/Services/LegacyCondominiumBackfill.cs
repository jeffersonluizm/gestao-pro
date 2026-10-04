using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public sealed record LegacyCondominiumBackfillResult(int VisitorsUpdated, int VisitorsLeft, int ParkingsUpdated, int ParkingsLeft);

// Preenche o condominiumId de visitantes e vagas antigos, gravados antes de a API exigir o campo.
// Sem ele, o controle de acesso não prova o condomínio e a administração recebe 404 ao editar.
// Idempotente: só olha registros sem condomínio e nunca altera um já preenchido.
// - Visitante: condomínio do morador (residentId).
// - Vaga: condomínio do morador, de quem reservou ou da unidade; sem vínculo, o único condomínio do sistema
//   (se houver mais de um, a vaga fica sem e entra na contagem do log).
public class LegacyCondominiumBackfill
{
    private readonly IVisitorRepository _visitors;
    private readonly IParkingRepository _parkings;
    private readonly IUserDirectory _users;
    private readonly IUnitService _units;
    private readonly ICondominiumService _condominiums;
    private readonly ILogger<LegacyCondominiumBackfill> _logger;
    private readonly Dictionary<string, string?> _userCondominiums = new();

    public LegacyCondominiumBackfill(
        IVisitorRepository visitors,
        IParkingRepository parkings,
        IUserDirectory users,
        IUnitService units,
        ICondominiumService condominiums,
        ILogger<LegacyCondominiumBackfill> logger)
    {
        _visitors = visitors;
        _parkings = parkings;
        _users = users;
        _units = units;
        _condominiums = condominiums;
        _logger = logger;
    }

    public async Task<LegacyCondominiumBackfillResult> RunAsync()
    {
        var (visitorsUpdated, visitorsLeft) = await BackfillVisitorsAsync();
        var (parkingsUpdated, parkingsLeft) = await BackfillParkingsAsync();

        var result = new LegacyCondominiumBackfillResult(visitorsUpdated, visitorsLeft, parkingsUpdated, parkingsLeft);
        if (visitorsUpdated + parkingsUpdated > 0)
            _logger.LogInformation(
                "Condomínio preenchido em {Visitors} visitante(s) e {Parkings} vaga(s) antigos.", visitorsUpdated, parkingsUpdated);
        if (visitorsLeft + parkingsLeft > 0)
            _logger.LogWarning(
                "Ficaram sem condomínio {Visitors} visitante(s) e {Parkings} vaga(s): não foi possível derivar. " +
                "A administração recebe 404 ao editá-los até que o campo condominiumId seja preenchido.",
                visitorsLeft, parkingsLeft);
        return result;
    }

    private async Task<(int Updated, int Left)> BackfillVisitorsAsync()
    {
        int updated = 0, left = 0;
        foreach (var visitor in (await _visitors.GetAllAsync()).Where(v => IsMissing(v.CondominiumId)))
        {
            var condominiumId = await CondominiumOfUserAsync(visitor.ResidentId);
            if (condominiumId is null)
            {
                left++;
                continue;
            }

            visitor.CondominiumId = condominiumId;
            await _visitors.UpdateAsync(visitor.Id!, visitor);
            updated++;
        }
        return (updated, left);
    }

    private async Task<(int Updated, int Left)> BackfillParkingsAsync()
    {
        int updated = 0, left = 0;
        string? singleCondominium = null;
        var singleCondominiumChecked = false;

        foreach (var parking in (await _parkings.GetAllAsync()).Where(p => IsMissing(p.CondominiumId)))
        {
            var condominiumId =
                await CondominiumOfUserAsync(parking.ResidentId) ??
                await CondominiumOfUserAsync(parking.ReservedByUserId) ??
                await CondominiumOfUnitAsync(parking.UnitId);

            if (condominiumId is null)
            {
                if (!singleCondominiumChecked)
                {
                    var all = await _condominiums.GetAllAsync();
                    singleCondominium = all.Count == 1 ? all[0].Id : null;
                    singleCondominiumChecked = true;
                }
                condominiumId = singleCondominium;
            }

            if (condominiumId is null)
            {
                left++;
                continue;
            }

            parking.CondominiumId = condominiumId;
            await _parkings.UpdateAsync(parking.Id!, parking);
            updated++;
        }
        return (updated, left);
    }

    private async Task<string?> CondominiumOfUserAsync(string? userId)
    {
        if (IsMissing(userId)) return null;
        if (!_userCondominiums.TryGetValue(userId!, out var condominiumId))
        {
            var user = await _users.GetByIdAsync(userId!);
            condominiumId = IsMissing(user?.CondominiumId) ? null : user!.CondominiumId;
            _userCondominiums[userId!] = condominiumId;
        }
        return condominiumId;
    }

    private async Task<string?> CondominiumOfUnitAsync(string? unitId)
    {
        if (IsMissing(unitId)) return null;
        var unit = await _units.GetByIdAsync(unitId!);
        return IsMissing(unit?.CondominiumId) ? null : unit!.CondominiumId;
    }

    private static bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value);
}

// Roda o preenchimento uma vez na partida, em segundo plano: não atrasa a subida da API e,
// se o banco estiver fora do ar ou algo falhar, só registra em log (a API continua de pé).
public class LegacyCondominiumBackfillInitializer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LegacyCondominiumBackfillInitializer> _logger;

    public LegacyCondominiumBackfillInitializer(
        IServiceScopeFactory scopeFactory,
        ILogger<LegacyCondominiumBackfillInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LegacyCondominiumBackfill>().RunAsync();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // API encerrando durante o preenchimento: ele roda de novo na próxima partida.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Falha ao preencher o condomínio de visitantes e vagas antigos. A API continuará disponível.");
        }
    }
}
