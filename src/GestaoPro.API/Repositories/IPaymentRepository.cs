using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IPaymentRepository
{
    Task RegisterPaymentAsync(PaymentModel payment);
    Task<PaymentModel?> GetByIdAsync(string id);
    Task<IEnumerable<PaymentModel>> GetByBillIdAsync(string billId);
    Task UpdateAsync(string id, PaymentModel payment);
}

