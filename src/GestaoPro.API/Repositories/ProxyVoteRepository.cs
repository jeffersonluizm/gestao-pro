using GestaoPro.API.Models;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class ProxyVoteRepository : IProxyVoteRepository
{
    private readonly IMongoCollection<ProxyVoteModel> _collection;

    public ProxyVoteRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ProxyVoteModel>("proxyVotes");
        EnsureUniqueIndex();
    }

    private void EnsureUniqueIndex()
    {
        var indexKeys = Builders<ProxyVoteModel>.IndexKeys
            .Ascending(p => p.AssemblyId)
            .Ascending(p => p.DelegatorUserId);
        var options = new CreateIndexOptions { Unique = true, Name = "IX_Unique_Proxy" };
        _collection.Indexes.CreateOne(new CreateIndexModel<ProxyVoteModel>(indexKeys, options));
    }

    public async Task<ProxyVoteModel> CreateAsync(ProxyVoteModel proxy, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(proxy, null, ct);
        return proxy;
    }

    public async Task<ProxyVoteModel?> GetByAssemblyAndDelegatorAsync(string assemblyId, string delegatorUserId, CancellationToken ct = default)
    {
        return await _collection.Find(p => p.AssemblyId == assemblyId && p.DelegatorUserId == delegatorUserId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ProxyVoteModel>> GetByAssemblyAsync(string assemblyId, CancellationToken ct = default)
    {
        return await _collection.Find(p => p.AssemblyId == assemblyId).ToListAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _collection.DeleteOneAsync(p => p.Id == id, ct);
    }
}