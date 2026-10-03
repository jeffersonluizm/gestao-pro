using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface IBillService
{
    Task<BillModel> CreateBillAsync(CreateBillDto dto);
    Task<IEnumerable<BillModel>> GetUserBillsAsync(string userId);
    Task<BillModel?> GetBillByIdAsync(string id);
    Task<BillModel?> UpdateBillAsync(string id, UpdateBillDto dto);
}

