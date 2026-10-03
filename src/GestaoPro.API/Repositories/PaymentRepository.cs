using GestaoPro.API.Models;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<PaymentModel> _payments;

    public PaymentRepository(IMongoDatabase database)
    {
        // Define a coleÃ§Ã£o "payments" no MongoDB
        _payments = database.GetCollection<PaymentModel>("payments");
    }

    public async Task RegisterPaymentAsync(PaymentModel payment)
    {
        await _payments.InsertOneAsync(payment);
    }

    public async Task<PaymentModel?> GetByIdAsync(string id) =>
        await _payments.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<IEnumerable<PaymentModel>> GetByBillIdAsync(string billId)
    {
        return await _payments.Find(p => p.BillId == billId).ToListAsync();
    }

    public async Task UpdateAsync(string id, PaymentModel payment) =>
        await _payments.ReplaceOneAsync(p => p.Id == id, payment);
}

