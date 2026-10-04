using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface IAnnouncementService
{
    Task<PagedResult<Announcement>> QueryAsync(AnnouncementQuery query);
    Task<Announcement?> GetByIdAsync(string id);
    Task<List<Announcement>> GetByCondominiumAsync(string condominiumId);
    Task<Announcement> CreateAsync(CreateAnnouncementDto dto);
    Task<Announcement?> UpdateAsync(string id, UpdateAnnouncementDto dto);
    Task<bool> DeleteAsync(string id);
}

