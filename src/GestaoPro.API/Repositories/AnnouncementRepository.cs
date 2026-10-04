using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly IMongoCollection<Announcement> _collection;

    // Usa o banco da aplicação (um pool de conexões só) e garante o índice das listas por condomínio.
    public AnnouncementRepository(IMongoDatabase database, IOptions<MongoDBSettings> settings)
    {
        _collection = database.GetCollection<Announcement>(settings.Value.AnnouncementsCollection);
        _collection.Indexes.CreateOne(new CreateIndexModel<Announcement>(
            Builders<Announcement>.IndexKeys.Ascending(a => a.CondominiumId).Descending(a => a.CreatedAt)));
    }

    // Filtro, ordem e página no MongoDB: a API não lê avisos de outros condomínios.
    public async Task<PagedResult<Announcement>> QueryAsync(AnnouncementQuery query)
    {
        var filter = Builders<Announcement>.Filter.Eq(a => a.CondominiumId, query.CondominiumId);
        if (query.ActiveOnly) filter &= Builders<Announcement>.Filter.Eq(a => a.IsActive, true);
        if (!string.IsNullOrWhiteSpace(query.Category)) filter &= Builders<Announcement>.Filter.Eq(a => a.Category, query.Category);

        var find = _collection.Find(filter).SortByDescending(a => a.CreatedAt);
        if (query.Page is not int page || query.PageSize is not int size)
        {
            var all = await find.ToListAsync();
            return new PagedResult<Announcement>(all, all.Count);
        }

        var items = await find.Skip((page - 1) * size).Limit(size).ToListAsync();
        var total = await _collection.CountDocumentsAsync(filter);
        return new PagedResult<Announcement>(items, total);
    }

    public async Task<Announcement?> GetByIdAsync(string id) =>
        await _collection.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<List<Announcement>> GetByCondominiumAsync(string condominiumId) =>
        await _collection.Find(a => a.CondominiumId == condominiumId).SortByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<Announcement> CreateAsync(Announcement announcement)
    {
        await _collection.InsertOneAsync(announcement);
        return announcement;
    }

    public async Task UpdateAsync(string id, Announcement announcement) =>
        await _collection.ReplaceOneAsync(a => a.Id == id, announcement);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(a => a.Id == id);
}

