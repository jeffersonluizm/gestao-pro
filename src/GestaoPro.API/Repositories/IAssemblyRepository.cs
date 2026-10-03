using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IAssemblyRepository
{
    Task<List<AssemblyModel>> GetAllAsync();
    Task<List<AssemblyModel>> GetHistoryAsync();
    Task<AssemblyModel?> GetByIdAsync(string id);
    Task<List<AssemblyModel>> GetByCondominiumAsync(string condominiumId);
    Task<List<AssemblyModel>> GetHistoryByCondominiumAsync(string condominiumId);
    Task<AssemblyModel> CreateAsync(AssemblyModel assembly);
    Task UpdateAsync(string id, AssemblyModel assembly);
    Task DeleteAsync(string id);
    Task SoftDeleteAsync(string id, string? deletedBy);
}

