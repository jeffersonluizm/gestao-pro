using GestaoPro.API.Models;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Services;

public class VoteIndexInitializer : IHostedService
{
    private const string CollectionName = "votes";
    private const string IndexName = "IX_Unique_Vote";

    private readonly IMongoDatabase _database;
    private readonly ILogger<VoteIndexInitializer> _logger;

    public VoteIndexInitializer(IMongoDatabase database, ILogger<VoteIndexInitializer> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<VoteModel>(CollectionName);

        try
        {
            var duplicates = await FindDuplicatesAsync(collection, cancellationToken);
            if (duplicates.Count > 0)
            {
                LogDuplicates(duplicates);
                return;
            }

            var indexes = await collection.Indexes.ListAsync(cancellationToken);
            var existingIndexes = await indexes.ToListAsync(cancellationToken);

            if (existingIndexes.Any(IsDesiredUniqueIndex))
            {
                _logger.LogInformation("Índice único de votos por assembleia, usuário e pauta já está configurado.");
                return;
            }

            var conflictingNamedIndex = existingIndexes.FirstOrDefault(index =>
                string.Equals(index.GetValue("name", "").AsString, IndexName, StringComparison.Ordinal));

            if (conflictingNamedIndex is not null)
            {
                await collection.Indexes.DropOneAsync(IndexName, cancellationToken);
                _logger.LogInformation("Índice legado {IndexName} removido para atualização.", IndexName);
            }

            var keys = Builders<VoteModel>.IndexKeys
                .Ascending(vote => vote.AssemblyId)
                .Ascending(vote => vote.UserId)
                .Ascending(vote => vote.TopicIndex);
            var options = new CreateIndexOptions
            {
                Unique = true,
                Name = IndexName
            };

            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<VoteModel>(keys, options),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Índice único {IndexName} criado em {CollectionName}.", IndexName, CollectionName);
        }
        catch (MongoCommandException exception) when (
            exception.Code is 85 or 86 or 11000 ||
            exception.CodeName is "IndexOptionsConflict" or "IndexKeySpecsConflict" or "DuplicateKey")
        {
            _logger.LogError(
                exception,
                "Não foi possível garantir o índice único de votos. Execute o diagnóstico de duplicidades antes da limpeza controlada.");
        }
        catch (MongoException exception)
        {
            _logger.LogError(exception, "Falha ao inicializar o índice único de votos. A API continuará disponível.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<List<BsonDocument>> FindDuplicatesAsync(
        IMongoCollection<VoteModel> collection,
        CancellationToken cancellationToken)
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument
                {
                    ["assemblyId"] = "$assemblyId",
                    ["userId"] = "$userId",
                    ["topicIndex"] = "$topicIndex"
                },
                ["count"] = new BsonDocument("$sum", 1),
                ["ids"] = new BsonDocument("$push", "$_id")
            }),
            new BsonDocument("$match", new BsonDocument("count", new BsonDocument("$gt", 1)))
        };

        return await collection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
    }

    private void LogDuplicates(IEnumerable<BsonDocument> duplicates)
    {
        foreach (var duplicate in duplicates)
        {
            _logger.LogError(
                "Votos duplicados impedem a criação do índice único. Chave: {Key}; quantidade: {Count}; IDs: {Ids}",
                duplicate["_id"].ToJson(),
                duplicate["count"].AsInt32,
                duplicate["ids"].ToJson());
        }
    }

    private static bool IsDesiredUniqueIndex(BsonDocument index)
    {
        if (!index.TryGetValue("unique", out var unique) || !unique.ToBoolean())
            return false;

        if (!index.TryGetValue("key", out var keyValue) || !keyValue.IsBsonDocument)
            return false;

        var key = keyValue.AsBsonDocument;
        return key.ElementCount == 3 &&
               key.TryGetValue("assemblyId", out var assemblyOrder) && assemblyOrder.ToInt32() == 1 &&
               key.TryGetValue("userId", out var userOrder) && userOrder.ToInt32() == 1 &&
               key.TryGetValue("topicIndex", out var topicOrder) && topicOrder.ToInt32() == 1;
    }
}
