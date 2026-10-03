using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

public class Reservation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("commonAreaId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CommonAreaId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("condominiumId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [BsonElement("date")]
    public string Date { get; set; } = string.Empty;

    [BsonElement("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [BsonElement("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = ReservationStatus.Pendente;

    [BsonElement("observations")]
    public string? Observations { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class ReservationStatus
{
    public const string Pendente = "pendente";
    public const string Aprovada = "aprovada";
    public const string Rejeitada = "rejeitada";
    public const string Cancelada = "cancelada";
}

