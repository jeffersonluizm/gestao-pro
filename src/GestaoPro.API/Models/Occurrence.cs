using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.Models;

public enum OccurrenceCategory
{
    Maintenance,
    Complaint,
    Suggestion
}

public enum OccurrenceStatus
{
    Open,
    InProgress,
    Concluded
}

public enum OccurrencePriority
{
    Low,
    Medium,
    High,
    Urgent
}

public class OccurrenceHistory
{
    public string Action { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonRepresentation(BsonType.ObjectId)]
    public string By { get; set; } = string.Empty;
}

public class Occurrence
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public OccurrenceCategory Category { get; set; } = OccurrenceCategory.Maintenance;
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Open;
    public OccurrencePriority Priority { get; set; } = OccurrencePriority.Medium;
    public List<OccurrenceHistory> History { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

