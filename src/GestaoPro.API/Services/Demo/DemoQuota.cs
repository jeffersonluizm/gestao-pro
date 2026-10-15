using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Services.Demo;

public interface IDemoRecordCounter
{
    // Quantos documentos da coleção pertencem ao condomínio demo (ou a coleção inteira, se ela não guarda condomínio).
    Task<long> CountAsync(string collection, string condominiumId, bool byCondominium, CancellationToken cancellationToken);
}

public sealed class MongoDemoRecordCounter : IDemoRecordCounter
{
    private readonly IMongoDatabase _database;

    public MongoDemoRecordCounter(IMongoDatabase database) => _database = database;

    public Task<long> CountAsync(string collection, string condominiumId, bool byCondominium, CancellationToken cancellationToken)
    {
        var filter = FilterDefinition<BsonDocument>.Empty;
        if (byCondominium)
        {
            // Os modelos gravam o condomínio ora como "condominiumId", ora como "CondominiumId"; ora ObjectId, ora texto.
            var ids = new BsonArray { condominiumId };
            if (ObjectId.TryParse(condominiumId, out var objectId)) ids.Add(objectId);
            filter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.In("condominiumId", ids),
                Builders<BsonDocument>.Filter.In("CondominiumId", ids));
        }
        return _database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}

// Teto de documentos por coleção no condomínio demo (Demo:LimitePorColecao, padrão 300). Acima dele, a criação
// responde 409 com mensagem amigável até o próximo reset. Sem o modo demonstração, não faz nada.
[AttributeUsage(AttributeTargets.Method)]
public sealed class DemoQuotaAttribute : TypeFilterAttribute
{
    public DemoQuotaAttribute(string collection, bool byCondominium = true) : base(typeof(DemoQuotaFilter))
    {
        Arguments = new object[] { collection, byCondominium };
    }
}

public sealed class DemoQuotaFilter : IAsyncActionFilter
{
    private readonly DemoCatalog _catalog;
    private readonly IDemoRecordCounter _counter;
    private readonly int _limit;
    private readonly int _resetMinutes;
    private readonly string _collection;
    private readonly bool _byCondominium;

    public DemoQuotaFilter(DemoCatalog catalog, IDemoRecordCounter counter, IConfiguration configuration, string collection, bool byCondominium)
    {
        _catalog = catalog;
        _counter = counter;
        _limit = Math.Max(1, configuration.GetValue("Demo:LimitePorColecao", 300));
        _resetMinutes = (int)DemoResetService.Interval(configuration).TotalMinutes;
        _collection = collection;
        _byCondominium = byCondominium;
    }

    public static string FullMessage(int resetMinutes) =>
        $"A demonstração chegou ao limite de registros deste tipo. Os dados voltam ao estado inicial a cada {resetMinutes} minutos; tente de novo depois.";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_catalog.Enabled && _catalog.CondominiumId is { } condominiumId &&
            await _counter.CountAsync(_collection, condominiumId, _byCondominium, context.HttpContext.RequestAborted) >= _limit)
        {
            context.Result = new ConflictObjectResult(new { message = FullMessage(_resetMinutes) });
            return;
        }

        await next();
    }
}
