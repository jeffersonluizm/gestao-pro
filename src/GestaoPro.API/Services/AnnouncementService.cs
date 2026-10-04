using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _repository;

    public AnnouncementService(IAnnouncementRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<Announcement>> QueryAsync(AnnouncementQuery query) => _repository.QueryAsync(query);

    public Task<Announcement?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<Announcement>> GetByCondominiumAsync(string condominiumId) =>
        _repository.GetByCondominiumAsync(condominiumId);

    public async Task<Announcement> CreateAsync(CreateAnnouncementDto dto)
    {
        var announcement = new Announcement
        {
            CondominiumId = dto.CondominiumId,
            AuthorId = dto.AuthorId,
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return await _repository.CreateAsync(announcement);
    }

    public async Task<Announcement?> UpdateAsync(string id, UpdateAnnouncementDto dto)
    {
        var announcement = await _repository.GetByIdAsync(id);
        if (announcement is null) return null;

        if (dto.Title is not null) announcement.Title = dto.Title;
        if (dto.Content is not null) announcement.Content = dto.Content;
        if (dto.Category is not null) announcement.Category = dto.Category;
        if (dto.IsActive.HasValue) announcement.IsActive = dto.IsActive.Value;
        if (dto.ExpiresAt.HasValue) announcement.ExpiresAt = dto.ExpiresAt.Value;

        announcement.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(id, announcement);
        return announcement;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var announcement = await _repository.GetByIdAsync(id);
        if (announcement is null) return false;

        await _repository.DeleteAsync(id);
        return true;
    }
}

