using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IAnnouncementRepository
{
    Task<List<Announcement>> GetAllAsync();
    Task<Announcement?> GetByIdAsync(string id);
    Task<List<Announcement>> GetByCondominiumAsync(string condominiumId);
    Task<List<Announcement>> GetActiveAsync();
    Task<List<Announcement>> GetByCategoryAsync(string category);
    Task<Announcement> CreateAsync(Announcement announcement);
    Task UpdateAsync(string id, Announcement announcement);
    Task DeleteAsync(string id);
}

