using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IAnnouncementRepository
{
    Task<PagedResult<Announcement>> QueryAsync(AnnouncementQuery query);
    Task<Announcement?> GetByIdAsync(string id);
    Task<List<Announcement>> GetByCondominiumAsync(string condominiumId);
    Task<Announcement> CreateAsync(Announcement announcement);
    Task UpdateAsync(string id, Announcement announcement);
    Task DeleteAsync(string id);
}

