using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface IAssemblyService
{
    Task<List<AssemblyModel>> GetAllAsync();
    Task<List<AssemblyModel>> GetHistoryAsync();
    Task<AssemblyModel?> GetByIdAsync(string id);
    Task<List<AssemblyModel>> GetByCondominiumAsync(string condominiumId);
    Task<List<AssemblyModel>> GetHistoryByCondominiumAsync(string condominiumId);
    Task<AssemblyModel> CreateAsync(CreateAssemblyDto dto);
    Task<AssemblyModel?> UpdateAsync(string id, UpdateAssemblyDto dto);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteAsync(string id, string? deletedBy);
}

