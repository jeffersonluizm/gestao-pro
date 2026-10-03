using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IParkingRepository
{
    Task<List<Parking>> GetAllAsync();
    Task<Parking?> GetByIdAsync(string id);
    Task<List<Parking>> GetAvailableAsync();
    Task<Parking> CreateAsync(Parking parking);
    Task UpdateAsync(string id, Parking parking);
    Task ReserveAsync(string id, string userId);
    Task UpdateStatusAsync(string id, string status);
    Task DeleteAsync(string id);
}

