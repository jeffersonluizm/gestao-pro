using GestaoPro.API.Services;
using MongoDB.Driver;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Repositories;

public class OccurrenceRepository
{
    private readonly IMongoCollection<Occurrence> _occurrences;

    public OccurrenceRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _occurrences = database.GetCollection<Occurrence>("occurrences");

        // Índices para consultas mais frequentes
        _occurrences.Indexes.CreateOne(new CreateIndexModel<Occurrence>(
            Builders<Occurrence>.IndexKeys
                .Ascending(o => o.CondominiumId)
                .Ascending(o => o.Status)));

        _occurrences.Indexes.CreateOne(new CreateIndexModel<Occurrence>(
            Builders<Occurrence>.IndexKeys
                .Ascending(o => o.UserId)
                .Descending(o => o.CreatedAt)));
    }

    public async Task<List<Occurrence>> GetAllByCondominiumAsync(string condominiumId) =>
        await _occurrences.Find(o => o.CondominiumId == condominiumId).ToListAsync();

    public async Task<List<Occurrence>> GetByCondominiumAndStatusAsync(
        string condominiumId, OccurrenceStatus status) =>
        await _occurrences.Find(o =>
            o.CondominiumId == condominiumId &&
            o.Status == status).ToListAsync();

    public async Task<List<Occurrence>> GetByUserAsync(string userId) =>
        await _occurrences.Find(o => o.UserId == userId).ToListAsync();

    public async Task<Occurrence?> GetByIdAsync(string id) =>
        await _occurrences.Find(o => o.Id == id).FirstOrDefaultAsync();

    public async Task<Occurrence> CreateAsync(Occurrence occurrence)
    {
        await _occurrences.InsertOneAsync(occurrence);
        return occurrence;
    }

    public async Task UpdateAsync(string id, Occurrence occurrence)
    {
        occurrence.UpdatedAt = DateTime.UtcNow;
        await _occurrences.ReplaceOneAsync(o => o.Id == id, occurrence);
    }

    public async Task AddHistoryAsync(string id, OccurrenceHistory history)
    {
        var update = Builders<Occurrence>.Update
            .PushEach(o => o.History, new[] { history }, slice: -HistoryLimit.Max)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        await _occurrences.UpdateOneAsync(o => o.Id == id, update);
    }

    public async Task UpdateStatusAsync(string id, OccurrenceStatus status, OccurrenceHistory history)
    {
        var update = Builders<Occurrence>.Update
            .Set(o => o.Status, status)
            .PushEach(o => o.History, new[] { history }, slice: -HistoryLimit.Max)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        await _occurrences.UpdateOneAsync(o => o.Id == id, update);
    }

    public async Task DeleteAsync(string id) =>
        await _occurrences.DeleteOneAsync(o => o.Id == id);
}

