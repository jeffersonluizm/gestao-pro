using GestaoPro.API.Models;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreateOccurrenceRequest(
    [StringLength(100)][Required] string CondominiumId,
    [StringLength(100)][Required] string UserId,
    [StringLength(200)][Required] string Title,
    [StringLength(4000)][Required] string Description,
    [Required] OccurrenceCategory Category,
    [Required] OccurrencePriority Priority
);

public record UpdateOccurrenceRequest(
    [StringLength(200)] string? Title,
    [StringLength(4000)] string? Description,
    OccurrenceCategory? Category,
    OccurrencePriority? Priority
);

public record UpdateOccurrenceStatusRequest(
    [Required] OccurrenceStatus Status,
    [StringLength(4000)][Required] string Note,
    [StringLength(100)][Required] string UpdatedByUserId
);

public class OccurrenceResponse
{
    public string Id { get; set; } = string.Empty;
    public string CondominiumId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OccurrenceCategory Category { get; set; }
    public OccurrenceStatus Status { get; set; }
    public OccurrencePriority Priority { get; set; }
    public List<OccurrenceHistoryResponse> History { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string TimeOpen =>
        Status != OccurrenceStatus.Concluded
            ? $"{(DateTime.UtcNow - CreatedAt).Days} dia(s)"
            : "Concluída";
}

public record OccurrenceHistoryResponse(
    string Action,
    string Note,
    DateTime Date,
    string By
);

