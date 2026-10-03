using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface IPaymentService
{
    Task<PaymentModel> ProcessPaymentAsync(CreatePaymentDto dto);
    Task<PaymentModel?> GetByIdAsync(string id);
    Task<IEnumerable<PaymentModel>> GetByBillIdAsync(string billId);
    Task<PaymentModel?> UpdateAsync(string id, UpdatePaymentDto dto);
}

