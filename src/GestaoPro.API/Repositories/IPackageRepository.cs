using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IPackageRepository
{
    Task<List<Package>> GetAllAsync();
    Task<Package?> GetByIdAsync(string id);
    Task<List<Package>> GetByCondominiumAsync(string condominiumId);
    Task<List<Package>> GetPendingByCondominiumAsync(string condominiumId);
    Task<Package> CreateAsync(Package package);
    Task UpdateAsync(string id, Package package);
    Task PickUpAsync(string id);
    Task DeleteAsync(string id);
}

