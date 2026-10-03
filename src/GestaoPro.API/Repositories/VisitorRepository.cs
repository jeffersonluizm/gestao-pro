using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;

namespace GestaoPro.API.Repositories;

public class VisitorRepository : IVisitorRepository
{
    private readonly IMongoCollection<Visitor> _collection;

    public VisitorRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<Visitor>(settings.Value.VisitorsCollection);
    }

    public async Task<List<Visitor>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<List<Visitor>> GetByResidentAsync(string residentId) =>
        await _collection.Find(x => x.ResidentId == residentId).ToListAsync();

    public async Task<Visitor?> GetByIdAsync(string id) =>
        ObjectId.TryParse(id, out _)
            ? await _collection.Find(x => x.Id == id).FirstOrDefaultAsync()
            : null;

    public async Task<Visitor> CreateAsync(Visitor visitor)
    {
        await _collection.InsertOneAsync(visitor);
        return visitor;
    }

    public async Task UpdateAsync(string id, Visitor visitor) =>
        await _collection.ReplaceOneAsync(x => x.Id == id, visitor);

    public async Task CheckInAsync(string id)
    {
        var now = BrasiliaNow();
        var update = Builders<Visitor>.Update
            .Set(x => x.Status, "checked-in")
            .Set(x => x.CheckInAt, now)
            .Push(x => x.History, new VisitorHistory
            {
                Action = "entrada",
                Status = "checked-in",
                Date = now
            });
        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task CheckOutAsync(string id)
    {
        var now = BrasiliaNow();
        var update = Builders<Visitor>.Update
            .Set(x => x.Status, "checked-out")
            .Set(x => x.CheckOutAt, now)
            .Push(x => x.History, new VisitorHistory
            {
                Action = "saida",
                Status = "checked-out",
                Date = now
            });
        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task DeleteAsync(string id)
    {
        if (ObjectId.TryParse(id, out _))
            await _collection.DeleteOneAsync(x => x.Id == id);
    }

    private static DateTime BrasiliaNow()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(-3);
        }
    }
}

