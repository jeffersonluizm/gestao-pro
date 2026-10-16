using GestaoPro.API.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;

namespace GestaoPro.API.Repositories;

public class ParkingRepository : IParkingRepository
{
    private readonly IMongoCollection<Parking> _collection;

    public ParkingRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<Parking>(settings.Value.ParkingCollection);
    }

    public async Task<List<Parking>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Parking?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<Parking>> GetAvailableAsync() =>
        await _collection.Find(x => x.Status == "available").ToListAsync();

    public async Task<Parking> CreateAsync(Parking parking)
    {
        await _collection.InsertOneAsync(parking);
        return parking;
    }

    public async Task UpdateAsync(string id, Parking parking) =>
        await _collection.ReplaceOneAsync(x => x.Id == id, parking);

    public async Task ReserveAsync(string id, string userId)
    {
        var now = BrazilTime.Now();
        var update = Builders<Parking>.Update
            .Set(x => x.Status, "reserved")
            .Set(x => x.ReservedByUserId, userId)
            .Set(x => x.ReservedAt, now)
            .PushEach(x => x.History, new[] { new ParkingHistory
            {
                Action = "entrada",
                Status = "reserved",
                UserId = userId,
                Date = now
            } }, slice: -HistoryLimit.Max);
        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task UpdateStatusAsync(string id, string status)
    {
        var now = BrazilTime.Now();
        var action = status == "available" ? "saida" : "entrada";
        var update = Builders<Parking>.Update
            .Set(x => x.Status, status)
            .PushEach(x => x.History, new[] { new ParkingHistory
            {
                Action = action,
                Status = status,
                Date = now
            } }, slice: -HistoryLimit.Max);

        if (status == "available")
        {
            update = update
                .Set(x => x.ReservedByUserId, null)
                .Set(x => x.ReservedAt, null);
        }

        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(x => x.Id == id);

}

