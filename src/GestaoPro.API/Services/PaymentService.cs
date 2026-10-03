using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBillRepository _billRepository;

    public PaymentService(IPaymentRepository paymentRepository, IBillRepository billRepository)
    {
        _paymentRepository = paymentRepository;
        _billRepository = billRepository;
    }

    public async Task<PaymentModel> ProcessPaymentAsync(CreatePaymentDto dto)
    {
        // 1. Criar o registro do pagamento
        var payment = new PaymentModel
        {
            BillId = dto.BillId,
            AmountPaid = dto.AmountPaid,
            PaymentMethod = dto.PaymentMethod,
            ReceiptUrl = dto.ReceiptUrl,
            PaymentDate = DateTime.UtcNow
        };

        // 2. Salvar o pagamento no banco
        await _paymentRepository.RegisterPaymentAsync(payment);

        // 3. REGRA DE OURO: Atualizar o status do boleto para "paid" (pago)
        await _billRepository.UpdateStatusAsync(dto.BillId, "paid");

        return payment;
    }

    public Task<PaymentModel?> GetByIdAsync(string id) =>
        _paymentRepository.GetByIdAsync(id);

    public Task<IEnumerable<PaymentModel>> GetByBillIdAsync(string billId) =>
        _paymentRepository.GetByBillIdAsync(billId);

    public async Task<PaymentModel?> UpdateAsync(string id, UpdatePaymentDto dto)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        if (payment is null) return null;

        if (dto.BillId is not null) payment.BillId = dto.BillId;
        if (dto.PaymentDate is not null) payment.PaymentDate = dto.PaymentDate.Value;
        if (dto.AmountPaid is not null) payment.AmountPaid = dto.AmountPaid.Value;
        if (dto.PaymentMethod is not null) payment.PaymentMethod = dto.PaymentMethod;
        if (dto.ReceiptUrl is not null) payment.ReceiptUrl = dto.ReceiptUrl;

        await _paymentRepository.UpdateAsync(id, payment);
        return payment;
    }
}

