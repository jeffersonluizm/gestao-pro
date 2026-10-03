using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IBillRepository
{
    Task CreateAsync(BillModel bill);
    Task<BillModel?> GetByIdAsync(string id);
    Task<IEnumerable<BillModel>> GetByUserIdAsync(string userId);
    Task UpdateAsync(string id, BillModel bill);
    Task UpdateStatusAsync(string id, string status);
    Task DeleteAsync(string id);
}

