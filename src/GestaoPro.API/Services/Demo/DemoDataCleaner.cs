using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Services.Demo;

// Reset destrutivo do modo demonstração: apaga tudo que pertence ao condomínio de demonstração (o que os
// visitantes criaram, inclusive contas ativadas com os CPFs pré-cadastrados) para o DataSeeder recriar do zero.
// Só toca no condomínio demo (identificado pelo CNPJ fixo da demonstração) e só roda com Demo:Enabled=true.
public sealed class DemoDataCleaner : IDemoCleaner
{
    private readonly IMongoDatabase _database;
    private readonly MongoDBSettings _settings;
    private readonly ILogger<DemoDataCleaner> _logger;

    public DemoDataCleaner(IMongoDatabase database, IOptions<MongoDBSettings> settings, ILogger<DemoDataCleaner> logger)
    {
        _database = database;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var condominium = await _database.GetCollection<BsonDocument>("condominiums")
            .Find(new BsonDocument("Cnpj", DemoCatalog.CondominiumCnpj))
            .FirstOrDefaultAsync(cancellationToken);
        if (condominium is null) return;

        var id = condominium["_id"].AsObjectId;
        // Os modelos gravam o condomínio ora como "condominiumId", ora como "CondominiumId"; ora ObjectId, ora texto.
        var ids = new BsonArray { id, id.ToString() };
        var ofCondominium = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.In("condominiumId", ids),
            Builders<BsonDocument>.Filter.In("CondominiumId", ids));

        async Task<BsonArray> IdsOf(string collection) => new(
            (await _database.GetCollection<BsonDocument>(collection).Find(ofCondominium)
                .Project(Builders<BsonDocument>.Projection.Include("_id")).ToListAsync(cancellationToken))
            .SelectMany(doc => new BsonValue[] { doc["_id"], doc["_id"].ToString()! }));

        var billIds = await IdsOf("bills");
        var assemblyIds = await IdsOf("assembly");

        long removed = 0;
        async Task Delete(string collection, FilterDefinition<BsonDocument> filter) =>
            removed += (await _database.GetCollection<BsonDocument>(collection).DeleteManyAsync(filter, cancellationToken)).DeletedCount;

        await Delete("payments", Builders<BsonDocument>.Filter.In("BillId", billIds));
        await Delete("votes", Builders<BsonDocument>.Filter.In("assemblyId", assemblyIds));
        await Delete("proxyVotes", Builders<BsonDocument>.Filter.In("assemblyId", assemblyIds));
        foreach (var collection in new[]
                 {
                     _settings.AnnouncementsCollection, _settings.CommonAreasCollection, _settings.ReservationsCollection,
                     _settings.VisitorsCollection, _settings.ParkingCollection, _settings.PackagesCollection,
                     "deliveries", "occurrences", "assembly", "bills", "pets", "units", "credentials", "users",
                 })
        {
            await Delete(collection, ofCondominium);
        }

        _logger.LogInformation("Modo demonstração: {Count} registros do condomínio demo apagados para recriar.", removed);
    }
}
