using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class OccurrenceAppService
{
    private readonly OccurrenceRepository _repository;

    public OccurrenceAppService(OccurrenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<OccurrenceResponse>> GetAllByCondominiumAsync(string condominiumId)
    {
        var occurrences = await _repository.GetAllByCondominiumAsync(condominiumId);
        return occurrences.Select(MapToResponse).ToList();
    }

    public async Task<List<OccurrenceResponse>> GetByCondominiumAndStatusAsync(
        string condominiumId, OccurrenceStatus status)
    {
        var occurrences = await _repository.GetByCondominiumAndStatusAsync(condominiumId, status);
        return occurrences.Select(MapToResponse).ToList();
    }

    public async Task<List<OccurrenceResponse>> GetByUserAsync(string userId)
    {
        var occurrences = await _repository.GetByUserAsync(userId);
        return occurrences.Select(MapToResponse).ToList();
    }

    public async Task<OccurrenceResponse?> GetByIdAsync(string id)
    {
        var occurrence = await _repository.GetByIdAsync(id);
        return occurrence is null ? null : MapToResponse(occurrence);
    }

    public async Task<OccurrenceResponse> CreateAsync(CreateOccurrenceRequest request)
    {
        var occurrence = new Occurrence
        {
            CondominiumId = request.CondominiumId,
            UserId = request.UserId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Status = OccurrenceStatus.Open,
            History = new List<OccurrenceHistory>
            {
                new OccurrenceHistory
                {
                    Action = "Created",
                    Note = "Occurrence registered by resident.",
                    Date = DateTime.UtcNow,
                    By = request.UserId
                }
            }
        };

        var created = await _repository.CreateAsync(occurrence);
        return MapToResponse(created);
    }

    public async Task<OccurrenceResponse?> UpdateAsync(string id, UpdateOccurrenceRequest request)
    {
        var occurrence = await _repository.GetByIdAsync(id);
        if (occurrence is null) return null;

        if (request.Title is not null) occurrence.Title = request.Title;
        if (request.Description is not null) occurrence.Description = request.Description;
        if (request.Category is not null) occurrence.Category = request.Category.Value;
        if (request.Priority is not null) occurrence.Priority = request.Priority.Value;

        await _repository.UpdateAsync(id, occurrence);
        return MapToResponse(occurrence);
    }

    public async Task<OccurrenceResponse?> UpdateStatusAsync(
        string id, UpdateOccurrenceStatusRequest request)
    {
        var occurrence = await _repository.GetByIdAsync(id);
        if (occurrence is null) return null;

        var history = new OccurrenceHistory
        {
            Action = "StatusUpdated",
            Note = request.Note,
            Date = DateTime.UtcNow,
            By = request.UpdatedByUserId
        };

        await _repository.UpdateStatusAsync(id, request.Status, history);
        occurrence.Status = request.Status;
        occurrence.History.Add(history);
        return MapToResponse(occurrence);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var occurrence = await _repository.GetByIdAsync(id);
        if (occurrence is null) return false;
        await _repository.DeleteAsync(id);
        return true;
    }

    private static OccurrenceResponse MapToResponse(Occurrence o) => new()
    {
        Id = o.Id!,
        CondominiumId = o.CondominiumId,
        UserId = o.UserId,
        Title = o.Title,
        Description = o.Description,
        Category = o.Category,
        Status = o.Status,
        Priority = o.Priority,
        History = o.History.Select(h => new OccurrenceHistoryResponse(
            h.Action,
            h.Note,
            h.Date,
            h.By
        )).ToList(),
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };
}

