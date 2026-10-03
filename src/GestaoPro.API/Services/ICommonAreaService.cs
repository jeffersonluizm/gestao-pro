using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface ICommonAreaService
{
    Task<List<CommonArea>> GetAllAsync();
    Task<List<CommonArea>> GetByCondominiumAsync(string condominiumId);
    Task<CommonArea?> GetByIdAsync(string id);
    Task<CommonArea> CreateAsync(CreateCommonAreaDto dto);
    Task<CommonArea?> UpdateAsync(string id, UpdateCommonAreaDto dto);
    Task<bool> DeleteAsync(string id);
}
