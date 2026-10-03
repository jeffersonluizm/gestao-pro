using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

public class Package
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("trackingCode")]
    public string TrackingCode { get; set; } = string.Empty;

    [BsonElement("carrier")]
    public string Carrier { get; set; } = string.Empty;

    [BsonElement("recipientId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RecipientId { get; set; } = string.Empty;

    [BsonElement("recipientName")]
    public string RecipientName { get; set; } = string.Empty;

    [BsonElement("unitNumber")]
    public string UnitNumber { get; set; } = string.Empty;

    [BsonElement("condominiumId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("pickedUpAt")]
    public DateTime? PickedUpAt { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    [BsonElement("observations")]
    public string Observations { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

