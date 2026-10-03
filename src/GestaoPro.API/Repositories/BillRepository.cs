using GestaoPro.API.Models;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class BillRepository : IBillRepository
{
    private readonly IMongoCollection<BillModel> _bills;

    public BillRepository(IMongoDatabase database)
    {
        // Define o nome da coleÃ§Ã£o no MongoDB
        _bills = database.GetCollection<BillModel>("bills");
    }

    public async Task CreateAsync(BillModel bill) =>
        await _bills.InsertOneAsync(bill);

    public async Task<BillModel?> GetByIdAsync(string id) =>
        await _bills.Find(b => b.Id == id).FirstOrDefaultAsync();

    public async Task<IEnumerable<BillModel>> GetByUserIdAsync(string userId) =>
        await _bills.Find(b => b.UserId == userId).ToListAsync();

    public async Task UpdateAsync(string id, BillModel bill)
    {
        bill.UpdatedAt = DateTime.UtcNow;
        await _bills.ReplaceOneAsync(b => b.Id == id, bill);
    }

    public async Task UpdateStatusAsync(string id, string status)
    {
        var update = Builders<BillModel>.Update
            .Set(b => b.Status, status)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        
        await _bills.UpdateOneAsync(b => b.Id == id, update);
    }

    public async Task DeleteAsync(string id) =>
        await _bills.DeleteOneAsync(b => b.Id == id);
}

