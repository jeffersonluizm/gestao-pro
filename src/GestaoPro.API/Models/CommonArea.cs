using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.Models;

public class CommonArea
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("condominiumId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("capacity")]
    public int Capacity { get; set; }

    [Required]
    [BsonElement("openTime")]
    public string OpenTime { get; set; } = "08:00";

    [Required]
    [BsonElement("closeTime")]
    public string CloseTime { get; set; } = "22:00";

    [BsonElement("rules")]
    public string Rules { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
