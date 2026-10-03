using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class BillService : IBillService
{
    private readonly IBillRepository _billRepository;

    public BillService(IBillRepository billRepository)
    {
        _billRepository = billRepository;
    }

    public async Task<BillModel> CreateBillAsync(CreateBillDto dto)
    {
        var bill = new BillModel
        {
            CondominiumId = dto.CondominiumId,
            UserId = dto.UserId,
            Description = dto.Description,
            Amount = dto.Amount,
            DueDate = dto.DueDate,
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode)
                ? $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
                : dto.Barcode,
            Status = "pending" // Todo boleto nasce pendente
        };

        await _billRepository.CreateAsync(bill);
        return bill;
    }

    public async Task<IEnumerable<BillModel>> GetUserBillsAsync(string userId) =>
        await _billRepository.GetByUserIdAsync(userId);

    public async Task<BillModel?> GetBillByIdAsync(string id) =>
        await _billRepository.GetByIdAsync(id);

    public async Task<BillModel?> UpdateBillAsync(string id, UpdateBillDto dto)
    {
        var bill = await _billRepository.GetByIdAsync(id);
        if (bill is null) return null;

        if (dto.CondominiumId is not null) bill.CondominiumId = dto.CondominiumId;
        if (dto.UserId is not null) bill.UserId = dto.UserId;
        if (dto.Description is not null) bill.Description = dto.Description;
        if (dto.Amount is not null) bill.Amount = dto.Amount.Value;
        if (dto.DueDate is not null) bill.DueDate = dto.DueDate.Value;
        if (dto.Barcode is not null) bill.Barcode = dto.Barcode;
        if (dto.Status is not null) bill.Status = dto.Status;

        await _billRepository.UpdateAsync(id, bill);
        return bill;
    }
}

