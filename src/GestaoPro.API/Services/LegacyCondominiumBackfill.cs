using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Services;

public sealed record LegacyCondominiumBackfillResult(int VisitorsUpdated, int VisitorsLeft, int ParkingsUpdated, int ParkingsLeft);

// Registros antigos sem condomínio: só o _id (como está no banco, mesmo que não seja ObjectId) e os vínculos.
public sealed record LegacyVisitor(BsonValue Id, string? ResidentId);

public sealed record LegacyParking(BsonValue Id, string? ResidentId, string? ReservedByUserId, string? UnitId);

// Acesso ao banco do preenchimento: lê só quem está sem condomínio e grava só o campo condominiumId,
// e só se ele continuar vazio (UpdateOne com filtro). Devolve false quando o filtro não casou.
public interface ILegacyCondominiumStore
{
    Task<List<LegacyVisitor>> GetVisitorsWithoutCondominiumAsync();
    Task<List<LegacyParking>> GetParkingsWithoutCondominiumAsync();
    Task<bool> SetVisitorCondominiumAsync(BsonValue id, string condominiumId);
    Task<bool> SetParkingCondominiumAsync(BsonValue id, string condominiumId);
}

// Trabalha com BsonDocument para não depender do mapeamento dos modelos: _id legado que não é ObjectId
// não quebra a leitura da lista nem a escrita.
public class MongoLegacyCondominiumStore : ILegacyCondominiumStore
{
    private readonly IMongoCollection<BsonDocument> _visitors;
    private readonly IMongoCollection<BsonDocument> _parkings;

    public MongoLegacyCondominiumStore(IMongoDatabase database, IOptions<MongoDBSettings> settings)
    {
        _visitors = database.GetCollection<BsonDocument>(settings.Value.VisitorsCollection);
        _parkings = database.GetCollection<BsonDocument>(settings.Value.ParkingCollection);
    }

    // condominiumId ausente, nulo, vazio ou só com espaços ($in com null também casa campo ausente).
    public static BsonDocument MissingCondominium() =>
        new("condominiumId", new BsonDocument("$in", new BsonArray { BsonNull.Value, new BsonRegularExpression(@"^\s*$") }));

    public static BsonDocument WithoutCondominium(BsonValue id) =>
        new BsonDocument("_id", id).AddRange(MissingCondominium());

    // Os modelos guardam condominiumId como ObjectId; valor que não é ObjectId vai como texto.
    public static BsonValue CondominiumValue(string condominiumId) =>
        ObjectId.TryParse(condominiumId, out var objectId) ? new BsonObjectId(objectId) : new BsonString(condominiumId);

    public async Task<List<LegacyVisitor>> GetVisitorsWithoutCondominiumAsync()
    {
        var documents = await _visitors.Find(MissingCondominium())
            .Project(Builders<BsonDocument>.Projection.Include("residentId"))
            .ToListAsync();
        return documents.Select(d => new LegacyVisitor(d["_id"], Text(d, "residentId"))).ToList();
    }

    public async Task<List<LegacyParking>> GetParkingsWithoutCondominiumAsync()
    {
        var documents = await _parkings.Find(MissingCondominium())
            .Project(Builders<BsonDocument>.Projection.Include("residentId").Include("reservedByUserId").Include("unitId"))
            .ToListAsync();
        return documents
            .Select(d => new LegacyParking(d["_id"], Text(d, "residentId"), Text(d, "reservedByUserId"), Text(d, "unitId")))
            .ToList();
    }

    public Task<bool> SetVisitorCondominiumAsync(BsonValue id, string condominiumId) =>
        SetCondominiumAsync(_visitors, id, condominiumId);

    public Task<bool> SetParkingCondominiumAsync(BsonValue id, string condominiumId) =>
        SetCondominiumAsync(_parkings, id, condominiumId);

    private static async Task<bool> SetCondominiumAsync(IMongoCollection<BsonDocument> collection, BsonValue id, string condominiumId)
    {
        var result = await collection.UpdateOneAsync(
            WithoutCondominium(id),
            new BsonDocument("$set", new BsonDocument("condominiumId", CondominiumValue(condominiumId))));
        return result.ModifiedCount > 0;
    }

    private static string? Text(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) && !value.IsBsonNull ? value.ToString() : null;
}

// Preenche o condominiumId de visitantes e vagas antigos, gravados antes de a API exigir o campo.
// Sem ele, o controle de acesso não prova o condomínio e a administração recebe 404 ao editar.
// Idempotente: só lê quem está sem condomínio e só grava se o campo continuar vazio.
// - Visitante: condomínio do morador (residentId).
// - Vaga: condomínio do morador, de quem reservou ou da unidade; sem vínculo, o único condomínio do sistema
//   (se houver mais de um, a vaga fica sem e entra na contagem do log).
// Cada registro é tratado à parte: erro em um (id legado, falha do banco) só conta como "ficou sem".
public class LegacyCondominiumBackfill
{
    private readonly ILegacyCondominiumStore _store;
    private readonly IUserDirectory _users;
    private readonly IUnitService _units;
    private readonly ICondominiumService _condominiums;
    private readonly ILogger<LegacyCondominiumBackfill> _logger;
    private readonly Dictionary<string, string?> _userCondominiums = new();
    private string? _singleCondominium;
    private bool _singleCondominiumChecked;

    public LegacyCondominiumBackfill(
        ILegacyCondominiumStore store,
        IUserDirectory users,
        IUnitService units,
        ICondominiumService condominiums,
        ILogger<LegacyCondominiumBackfill> logger)
    {
        _store = store;
        _users = users;
        _units = units;
        _condominiums = condominiums;
        _logger = logger;
    }

    public async Task<LegacyCondominiumBackfillResult> RunAsync()
    {
        int visitorsUpdated = 0, visitorsLeft = 0, parkingsUpdated = 0, parkingsLeft = 0;

        foreach (var visitor in await _store.GetVisitorsWithoutCondominiumAsync())
        {
            var outcome = await TryFillAsync("visitante", visitor.Id,
                () => CondominiumOfUserAsync(visitor.ResidentId),
                condominiumId => _store.SetVisitorCondominiumAsync(visitor.Id, condominiumId));
            if (outcome == Outcome.Updated) visitorsUpdated++;
            else if (outcome == Outcome.Left) visitorsLeft++;
        }

        foreach (var parking in await _store.GetParkingsWithoutCondominiumAsync())
        {
            var outcome = await TryFillAsync("vaga", parking.Id,
                () => CondominiumOfParkingAsync(parking),
                condominiumId => _store.SetParkingCondominiumAsync(parking.Id, condominiumId));
            if (outcome == Outcome.Updated) parkingsUpdated++;
            else if (outcome == Outcome.Left) parkingsLeft++;
        }

        if (visitorsUpdated + parkingsUpdated > 0)
            _logger.LogInformation(
                "Condomínio preenchido em {Visitors} visitante(s) e {Parkings} vaga(s) antigos.", visitorsUpdated, parkingsUpdated);
        if (visitorsLeft + parkingsLeft > 0)
            _logger.LogWarning(
                "Ficaram sem condomínio {Visitors} visitante(s) e {Parkings} vaga(s): não foi possível derivar ou gravar. " +
                "A administração recebe 404 ao editá-los até que o campo condominiumId seja preenchido.",
                visitorsLeft, parkingsLeft);

        return new LegacyCondominiumBackfillResult(visitorsUpdated, visitorsLeft, parkingsUpdated, parkingsLeft);
    }

    private enum Outcome { Updated, Left, AlreadyFilled }

    private async Task<Outcome> TryFillAsync(string kind, BsonValue id, Func<Task<string?>> derive, Func<string, Task<bool>> save)
    {
        try
        {
            var condominiumId = await derive();
            if (condominiumId is null) return Outcome.Left;

            // false: outro processo preencheu o campo entre a leitura e a escrita.
            return await save(condominiumId) ? Outcome.Updated : Outcome.AlreadyFilled;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Não foi possível preencher o condomínio do(a) {Kind} {Id}.", kind, id.ToString());
            return Outcome.Left;
        }
    }

    private async Task<string?> CondominiumOfParkingAsync(LegacyParking parking) =>
        await CondominiumOfUserAsync(parking.ResidentId) ??
        await CondominiumOfUserAsync(parking.ReservedByUserId) ??
        await CondominiumOfUnitAsync(parking.UnitId) ??
        await SingleCondominiumAsync();

    private async Task<string?> SingleCondominiumAsync()
    {
        if (!_singleCondominiumChecked)
        {
            var all = await _condominiums.GetPublicAsync();
            _singleCondominium = all.Count == 1 ? all[0].Id : null;
            _singleCondominiumChecked = true;
        }
        return _singleCondominium;
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
