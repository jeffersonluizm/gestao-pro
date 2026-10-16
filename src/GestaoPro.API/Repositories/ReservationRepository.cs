using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly IMongoCollection<Reservation> _collection;

    // Banco da aplicação e índices das consultas usadas: por condomínio, por morador e por área e data (conflito).
    public ReservationRepository(IMongoDatabase database, IOptions<MongoDBSettings> settings)
    {
        _collection = database.GetCollection<Reservation>(settings.Value.ReservationsCollection);
        _collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Reservation>(Builders<Reservation>.IndexKeys.Ascending(r => r.CondominiumId).Ascending(r => r.Date)),
            new CreateIndexModel<Reservation>(Builders<Reservation>.IndexKeys.Ascending(r => r.UserId)),
            new CreateIndexModel<Reservation>(Builders<Reservation>.IndexKeys.Ascending(r => r.CommonAreaId).Ascending(r => r.Date)),
        });
    }

    public async Task<List<Reservation>> GetAllAsync() =>
        await _collection.Find(_ => true).SortByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<Reservation?> GetByIdAsync(string id) =>
        await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<List<Reservation>> GetByCondominiumAsync(string condominiumId) =>
        await _collection.Find(r => r.CondominiumId == condominiumId).SortBy(r => r.Date).ToListAsync();

    public async Task<List<Reservation>> GetByUserAsync(string userId) =>
        await _collection.Find(r => r.UserId == userId).SortBy(r => r.Date).ToListAsync();

    public async Task<List<Reservation>> GetByCommonAreaAsync(string commonAreaId) =>
        await _collection.Find(r => r.CommonAreaId == commonAreaId).SortBy(r => r.Date).ToListAsync();

    public async Task<List<Reservation>> GetByStatusAsync(string status) =>
        await _collection.Find(r => r.Status == status).SortBy(r => r.Date).ToListAsync();

    public async Task<List<Reservation>> GetByDateAsync(string date) =>
        await _collection.Find(r => r.Date == date).SortBy(r => r.StartTime).ToListAsync();

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        await _collection.InsertOneAsync(reservation);
        return reservation;
    }

    public async Task UpdateAsync(string id, Reservation reservation) =>
        await _collection.ReplaceOneAsync(r => r.Id == id, reservation);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(r => r.Id == id);
}

