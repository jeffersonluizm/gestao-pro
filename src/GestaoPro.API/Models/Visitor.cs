using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models
{
    public class Visitor
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("document")]
        public string Document { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("residentId")]
        public string ResidentId { get; set; } = string.Empty;

        [BsonElement("condominiumId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CondominiumId { get; set; }

        [BsonElement("unitId")]
        public string UnitId { get; set; } = string.Empty;

        [BsonElement("reason")]
        public string Reason { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = "pending";

        [BsonElement("checkInAt")]
        public DateTime? CheckInAt { get; set; }

        [BsonElement("checkOutAt")]
        public DateTime? CheckOutAt { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("history")]
        public List<VisitorHistory> History { get; set; } = new();
    }

    public class VisitorHistory
    {
        [BsonElement("action")]
        public string Action { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date { get; set; }
    }
}

