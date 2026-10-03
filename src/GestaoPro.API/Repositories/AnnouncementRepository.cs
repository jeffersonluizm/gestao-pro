using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly IMongoCollection<Announcement> _collection;

    public AnnouncementRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<Announcement>(settings.Value.AnnouncementsCollection);
    }

    public async Task<List<Announcement>> GetAllAsync() =>
        await _collection.Find(_ => true).SortByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<Announcement?> GetByIdAsync(string id) =>
        await _collection.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<List<Announcement>> GetByCondominiumAsync(string condominiumId) =>
        await _collection.Find(a => a.CondominiumId == condominiumId).SortByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<List<Announcement>> GetActiveAsync() =>
        await _collection.Find(a => a.IsActive).SortByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<List<Announcement>> GetByCategoryAsync(string category) =>
        await _collection.Find(a => a.Category == category).SortByDescending(a => a.CreatedAt).ToListAsync();

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

