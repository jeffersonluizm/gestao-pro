using GestaoPro.API.Models;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class AssemblyRepository : IAssemblyRepository
{
    private readonly IMongoCollection<AssemblyModel> _collection;

    public AssemblyRepository(IMongoDatabase database)
    {
        // "assemblies" Ã© o nome da coleÃ§Ã£o no banco
        _collection = database.GetCollection<AssemblyModel>("assembly");
        // Listas por condomínio (atuais e histórico).
        _collection.Indexes.CreateOne(new CreateIndexModel<AssemblyModel>(
            Builders<AssemblyModel>.IndexKeys.Ascending(a => a.CondominiumId)));
    }

    public async Task<List<AssemblyModel>> GetAllAsync() =>
        await _collection.Find(MainListFilter()).SortByDescending(a => a.StartTime).ToListAsync();

    public async Task<List<AssemblyModel>> GetHistoryAsync() =>
        await _collection.Find(HistoryFilter()).SortByDescending(a => a.EndTime).ToListAsync();

    public async Task<AssemblyModel?> GetByIdAsync(string id) =>
        await _collection.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<List<AssemblyModel>> GetByCondominiumAsync(string condominiumId) =>
        await _collection.Find(MainListFilter() & Builders<AssemblyModel>.Filter.Eq(a => a.CondominiumId, condominiumId))
            .SortByDescending(a => a.StartTime)
            .ToListAsync();

    public async Task<List<AssemblyModel>> GetHistoryByCondominiumAsync(string condominiumId) =>
        await _collection.Find(HistoryFilter() & Builders<AssemblyModel>.Filter.Eq(a => a.CondominiumId, condominiumId))
            .SortByDescending(a => a.EndTime)
            .ToListAsync();

    public async Task<AssemblyModel> CreateAsync(AssemblyModel assembly)
    {
        await _collection.InsertOneAsync(assembly);
        return assembly;
    }

    public async Task UpdateAsync(string id, AssemblyModel assembly) =>
        await _collection.ReplaceOneAsync(a => a.Id == id, assembly);

    public async Task DeleteAsync(string id) =>
        await SoftDeleteAsync(id, null);

    public async Task SoftDeleteAsync(string id, string? deletedBy)
    {
        var update = Builders<AssemblyModel>.Update
            .Set(a => a.IsDeleted, true)
            .Set(a => a.DeletedAt, DateTime.UtcNow)
            .Set(a => a.DeletedBy, deletedBy)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(a => a.Id == id && !a.IsDeleted, update);
    }

    private static FilterDefinition<AssemblyModel> MainListFilter()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        return Builders<AssemblyModel>.Filter.Ne(a => a.IsDeleted, true)
            & Builders<AssemblyModel>.Filter.Gte(a => a.EndTime, cutoff);
    }

    private static FilterDefinition<AssemblyModel> HistoryFilter()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        return Builders<AssemblyModel>.Filter.Ne(a => a.IsDeleted, true)
            & Builders<AssemblyModel>.Filter.Lt(a => a.EndTime, cutoff);
    }
}

