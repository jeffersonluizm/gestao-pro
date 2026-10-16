using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class CommonAreaRepository : ICommonAreaRepository
{
    private readonly IMongoCollection<CommonArea> _collection;

    // Banco da aplicação (um pool de conexões só) e índice da lista por condomínio.
    public CommonAreaRepository(IMongoDatabase database, IOptions<MongoDBSettings> settings)
    {
        _collection = database.GetCollection<CommonArea>(settings.Value.CommonAreasCollection);
        _collection.Indexes.CreateOne(new CreateIndexModel<CommonArea>(
            Builders<CommonArea>.IndexKeys.Ascending(a => a.CondominiumId).Ascending(a => a.Name)));
    }

    public async Task<List<CommonArea>> GetAllAsync() =>
        await _collection.Find(_ => true).SortBy(a => a.Name).ToListAsync();

    public async Task<List<CommonArea>> GetByCondominiumAsync(string condominiumId) =>
        await _collection.Find(a => a.CondominiumId == condominiumId).SortBy(a => a.Name).ToListAsync();

    public async Task<CommonArea?> GetByIdAsync(string id) =>
        await _collection.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<CommonArea> CreateAsync(CommonArea commonArea)
    {
        await _collection.InsertOneAsync(commonArea);
        return commonArea;
    }

    public async Task UpdateAsync(string id, CommonArea commonArea) =>
        await _collection.ReplaceOneAsync(a => a.Id == id, commonArea);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(a => a.Id == id);
}
