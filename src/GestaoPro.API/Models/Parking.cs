using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models
{
    public class Parking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("number")]
        public string Number { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("condominiumId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CondominiumId { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "available";

        [BsonElement("isShared")]
        public bool IsShared { get; set; }

        [BsonElement("residentId")]
        public string? ResidentId { get; set; }

        [BsonElement("unitId")]
        public string? UnitId { get; set; }

        [BsonElement("reservedByUserId")]
        public string? ReservedByUserId { get; set; }

        [BsonElement("reservedAt")]
        public DateTime? ReservedAt { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("history")]
        public List<ParkingHistory> History { get; set; } = new();
    }

    public class ParkingHistory
    {
        [BsonElement("action")]
        public string Action { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string? UserId { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }
    }
}

