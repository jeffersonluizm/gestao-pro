using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class CommonAreaService : ICommonAreaService
{
    private readonly ICommonAreaRepository _repository;

    public CommonAreaService(ICommonAreaRepository repository)
    {
        _repository = repository;
    }

    public Task<List<CommonArea>> GetAllAsync() => _repository.GetAllAsync();

    public Task<List<CommonArea>> GetByCondominiumAsync(string condominiumId) =>
        _repository.GetByCondominiumAsync(condominiumId);

    public Task<CommonArea?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public async Task<CommonArea> CreateAsync(CreateCommonAreaDto dto)
    {
        var commonArea = new CommonArea
        {
            CondominiumId = dto.CondominiumId,
            Name = dto.Name,
            Description = dto.Description,
            Capacity = dto.Capacity,
            OpenTime = dto.OpenTime,
            CloseTime = dto.CloseTime,
            Rules = dto.Rules,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(commonArea);
    }

    public async Task<CommonArea?> UpdateAsync(string id, UpdateCommonAreaDto dto)
    {
        var commonArea = await _repository.GetByIdAsync(id);
        if (commonArea is null) return null;

        if (dto.Name is not null) commonArea.Name = dto.Name;
        if (dto.Description is not null) commonArea.Description = dto.Description;
        if (dto.Capacity is not null) commonArea.Capacity = dto.Capacity.Value;
        if (dto.OpenTime is not null) commonArea.OpenTime = dto.OpenTime;
        if (dto.CloseTime is not null) commonArea.CloseTime = dto.CloseTime;
        if (dto.Rules is not null) commonArea.Rules = dto.Rules;
        if (dto.IsActive is not null) commonArea.IsActive = dto.IsActive.Value;

        commonArea.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(id, commonArea);
        return commonArea;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var commonArea = await _repository.GetByIdAsync(id);
        if (commonArea is null) return false;

        await _repository.DeleteAsync(id);
        return true;
    }
}
