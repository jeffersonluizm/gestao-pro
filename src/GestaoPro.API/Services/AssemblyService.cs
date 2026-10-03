using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class AssemblyService : IAssemblyService
{
    private readonly IAssemblyRepository _repository;

    public AssemblyService(IAssemblyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<AssemblyModel>> GetAllAsync() => _repository.GetAllAsync();

    public Task<List<AssemblyModel>> GetHistoryAsync() => _repository.GetHistoryAsync();

    public Task<AssemblyModel?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<AssemblyModel>> GetByCondominiumAsync(string condominiumId) =>
        _repository.GetByCondominiumAsync(condominiumId);

    public Task<List<AssemblyModel>> GetHistoryByCondominiumAsync(string condominiumId) =>
        _repository.GetHistoryByCondominiumAsync(condominiumId);

    public async Task<AssemblyModel> CreateAsync(CreateAssemblyDto dto)
    {
        var startDate = dto.StartDate ?? dto.ScheduledDate;
        var endDate = dto.EndDate ?? startDate.Date.AddDays(1).AddTicks(-1);

        if (endDate < startDate)
            throw new InvalidOperationException("A data final deve ser igual ou posterior à data inicial.");
        EnsureVotingPeriodHasNotEnded(endDate);

        var assembly = new AssemblyModel
        {
            CondominiumId = dto.CondominiumId,
            CreatedBy = string.IsNullOrWhiteSpace(dto.CreatedBy) ? null : dto.CreatedBy,
            Title = dto.Title,
            Description = dto.Description,
            StartTime = NormalizeUtc(startDate),
            EndTime = NormalizeUtc(endDate),
            Status = GetStatusForPeriod(startDate, endDate),
            Topics = NormalizeTopics(dto.Topics, dto.TopicsText),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(assembly);
    }

    public async Task<AssemblyModel?> UpdateAsync(string id, UpdateAssemblyDto dto)
    {
        var assembly = await _repository.GetByIdAsync(id);
        if (assembly is null) return null;

        if (dto.Title is not null) assembly.Title = dto.Title;
        if (dto.Description is not null) assembly.Description = dto.Description;
        if (dto.StartDate.HasValue || dto.EndDate.HasValue || dto.ScheduledDate.HasValue)
        {
            var startDate = dto.StartDate ?? (dto.ScheduledDate ?? assembly.StartTime);
            var endDate = dto.EndDate ?? (dto.ScheduledDate?.Date.AddDays(1).AddTicks(-1) ?? assembly.EndTime);
            assembly.StartTime = NormalizeUtc(startDate);
            assembly.EndTime = NormalizeUtc(endDate);
            if (assembly.EndTime < assembly.StartTime)
                throw new InvalidOperationException("A data final deve ser igual ou posterior à data inicial.");
            EnsureVotingPeriodHasNotEnded(assembly.EndTime);
        }
        if (dto.Status is not null) assembly.Status = dto.Status;
        if (dto.Minutes is not null) assembly.Minutes = dto.Minutes;

        if (dto.Topics is not null || !string.IsNullOrWhiteSpace(dto.TopicsText))
            assembly.Topics = NormalizeTopics(dto.Topics, dto.TopicsText);

        assembly.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(id, assembly);
        return assembly;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var assembly = await _repository.GetByIdAsync(id);
        if (assembly is null || assembly.IsDeleted) return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, string? deletedBy)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var assembly = await _repository.GetByIdAsync(id);
        if (assembly is null || assembly.IsDeleted) return false;

        await _repository.SoftDeleteAsync(id, deletedBy);
        return true;
    }

    private static List<TopicModel> NormalizeTopics(IEnumerable<CreateTopicDto>? topics, string? topicsText)
    {
        if (topics is not null && topics.Any())
            return topics.Select(t => new TopicModel { Title = t.Title, Desc = t.Desc, VoteOptions = t.VoteOptions }).ToList();

        if (!string.IsNullOrWhiteSpace(topicsText))
            return topicsText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Select(title => new TopicModel { Title = title, Desc = string.Empty, VoteOptions = new List<string> { "sim", "não" } })
                .ToList();

        return new List<TopicModel>();
    }

    private static void EnsureVotingPeriodHasNotEnded(DateTime endDate)
    {
        if (NormalizeUtc(endDate) < DateTime.UtcNow)
            throw new InvalidOperationException("Não é permitido registrar uma assembleia cujo período de votação já foi encerrado.");
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string GetStatusForPeriod(DateTime startDate, DateTime endDate)
    {
        var now = DateTime.UtcNow;
        if (now < NormalizeUtc(startDate)) return "agendada";
        return now <= NormalizeUtc(endDate) ? "em_andamento" : "encerrada";
    }
}

