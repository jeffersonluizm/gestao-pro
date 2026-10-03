using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IVisitorRepository
{
    Task<List<Visitor>> GetAllAsync();
    Task<List<Visitor>> GetByResidentAsync(string residentId);
    Task<Visitor?> GetByIdAsync(string id);
    Task<Visitor> CreateAsync(Visitor visitor);
    Task UpdateAsync(string id, Visitor visitor);
    Task CheckInAsync(string id);
    Task CheckOutAsync(string id);
    Task DeleteAsync(string id);
}

