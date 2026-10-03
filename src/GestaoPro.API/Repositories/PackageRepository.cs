using Microsoft.Extensions.Options;
using MongoDB.Driver;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;

namespace GestaoPro.API.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly IMongoCollection<Package> _collection;

    public PackageRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<Package>(settings.Value.PackagesCollection);
    }

    public async Task<List<Package>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Package?> GetByIdAsync(string id) =>
        await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<List<Package>> GetByCondominiumAsync(string condominiumId) =>
        await _collection.Find(p => p.CondominiumId == condominiumId).ToListAsync();

    public async Task<List<Package>> GetPendingByCondominiumAsync(string condominiumId) =>
        await _collection.Find(p => p.CondominiumId == condominiumId && p.Status == "pending").ToListAsync();

    public async Task<Package> CreateAsync(Package package)
    {
        await _collection.InsertOneAsync(package);
        return package;
    }

    public async Task UpdateAsync(string id, Package package) =>
        await _collection.ReplaceOneAsync(p => p.Id == id, package);

    public async Task PickUpAsync(string id)
    {
        var update = Builders<Package>.Update
            .Set(p => p.Status, "picked_up")
            .Set(p => p.PickedUpAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(p => p.Id == id, update);
    }

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(p => p.Id == id);
}

