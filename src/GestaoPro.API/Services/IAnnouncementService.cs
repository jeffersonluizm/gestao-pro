using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface IAnnouncementService
{
    Task<List<Announcement>> GetAllAsync();
    Task<Announcement?> GetByIdAsync(string id);
    Task<List<Announcement>> GetByCondominiumAsync(string condominiumId);
    Task<List<Announcement>> GetActiveAsync();
    Task<List<Announcement>> GetByCategoryAsync(string category);
    Task<Announcement> CreateAsync(CreateAnnouncementDto dto);
    Task<Announcement?> UpdateAsync(string id, UpdateAnnouncementDto dto);
    Task<bool> DeleteAsync(string id);
}

