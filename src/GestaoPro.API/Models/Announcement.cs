using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

public class Announcement
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("condominiumId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [BsonElement("authorId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

