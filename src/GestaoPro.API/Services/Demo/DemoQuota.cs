using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Services.Demo;

public interface IDemoRecordCounter
{
    // Quantos documentos da coleção pertencem ao condomínio demo (ou a coleção inteira, se ela não guarda condomínio).
    Task<long> CountAsync(string collection, string condominiumId, bool byCondominium, CancellationToken cancellationToken);

    // Uma criação aprovada: quem guarda a contagem soma 1 (o teto fica exato dentro da validade do cache).
    void NoteCreated(string collection, string condominiumId, bool byCondominium) { }
}

public sealed class MongoDemoRecordCounter : IDemoRecordCounter
{
    private readonly IMongoDatabase _database;
    private readonly long _limit;

    public MongoDemoRecordCounter(IMongoDatabase database, IConfiguration configuration)
    {
        _database = database;
        _limit = DemoQuotaFilter.Limit(configuration);
    }

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
        // Conta só até o teto: o custo não cresce com o tamanho da coleção.
        return _database.GetCollection<BsonDocument>(collection)
            .CountDocumentsAsync(filter, new CountOptions { Limit = _limit }, cancellationToken);
    }
}

// Guarda a contagem por alguns segundos (por coleção e condomínio): sem isso, cada criação ia ao banco contar
// (no teste de estresse, H5 caiu de cerca de 7.500 para 130 req/s). O excesso possível fica limitado ao que o
// teto de escritas por minuto deixa passar nesse intervalo.
public sealed class CachedDemoRecordCounter : IDemoRecordCounter
{
    private readonly IDemoRecordCounter _inner;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _ttl;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (long Count, DateTimeOffset Until)> _cache = new();

    public CachedDemoRecordCounter(IDemoRecordCounter inner, TimeProvider clock, TimeSpan ttl)
    {
        _inner = inner;
        _clock = clock;
        _ttl = ttl;
    }

    public async Task<long> CountAsync(string collection, string condominiumId, bool byCondominium, CancellationToken cancellationToken)
    {
        var key = $"{collection}|{condominiumId}|{byCondominium}";
        var now = _clock.GetUtcNow();
        if (_cache.TryGetValue(key, out var cached) && cached.Until > now) return cached.Count;

        var count = await _inner.CountAsync(collection, condominiumId, byCondominium, cancellationToken);
        _cache[key] = (count, now + _ttl);
        return count;
    }

    public void NoteCreated(string collection, string condominiumId, bool byCondominium)
    {
        var key = $"{collection}|{condominiumId}|{byCondominium}";
        _cache.AddOrUpdate(key, _ => (0, DateTimeOffset.MinValue), (_, cached) => (cached.Count + 1, cached.Until));
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
        _limit = (int)Limit(configuration);
        _resetMinutes = (int)DemoResetService.Interval(configuration).TotalMinutes;
        _collection = collection;
        _byCondominium = byCondominium;
    }

    public static long Limit(IConfiguration configuration) => Math.Max(1, configuration.GetValue("Demo:LimitePorColecao", 300));

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

        var executed = await next();
        if (_catalog.Enabled && _catalog.CondominiumId is { } created && executed.Exception is null &&
            executed.Result is ObjectResult { StatusCode: >= 200 and < 300 } or StatusCodeResult { StatusCode: >= 200 and < 300 } or OkResult or CreatedResult or CreatedAtActionResult or NoContentResult)
            _counter.NoteCreated(_collection, created, _byCondominium);
    }
}
