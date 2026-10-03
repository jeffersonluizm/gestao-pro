using MongoDB.Driver;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Repositories;

public class PetRepository
{
    private readonly IMongoCollection<Pet> _pets;

    public PetRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _pets = database.GetCollection<Pet>("pets");

        _pets.Indexes.CreateOne(new CreateIndexModel<Pet>(
            Builders<Pet>.IndexKeys.Ascending(p => p.UnitId)));

        _pets.Indexes.CreateOne(new CreateIndexModel<Pet>(
            Builders<Pet>.IndexKeys.Ascending(p => p.OwnerId)));

        _pets.Indexes.CreateOne(new CreateIndexModel<Pet>(
            Builders<Pet>.IndexKeys.Ascending(p => p.CondominiumId)));
    }

    public async Task<List<Pet>> GetAllByCondominiumAsync(string condominiumId) =>
        await _pets.Find(p => p.CondominiumId == condominiumId && p.IsActive).ToListAsync();

    public async Task<List<Pet>> GetByUnitAsync(string unitId) =>
        await _pets.Find(p => p.UnitId == unitId && p.IsActive).ToListAsync();

    public async Task<List<Pet>> GetByOwnerAsync(string ownerId) =>
        await _pets.Find(p => p.OwnerId == ownerId && p.IsActive).ToListAsync();

    public async Task<Pet?> GetByIdAsync(string id) =>
        await _pets.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<Pet> CreateAsync(Pet pet)
    {
        await _pets.InsertOneAsync(pet);
        return pet;
    }

    public async Task UpdateAsync(string id, Pet pet)
    {
        pet.UpdatedAt = DateTime.UtcNow;
        await _pets.ReplaceOneAsync(p => p.Id == id, pet);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _pets.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }
}
