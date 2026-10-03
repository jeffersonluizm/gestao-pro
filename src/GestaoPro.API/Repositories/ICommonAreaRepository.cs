using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface ICommonAreaRepository
{
    Task<List<CommonArea>> GetAllAsync();
    Task<List<CommonArea>> GetByCondominiumAsync(string condominiumId);
    Task<CommonArea?> GetByIdAsync(string id);
    Task<CommonArea> CreateAsync(CommonArea commonArea);
    Task UpdateAsync(string id, CommonArea commonArea);
    Task DeleteAsync(string id);
}
