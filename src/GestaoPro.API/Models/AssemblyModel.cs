using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

[BsonIgnoreExtraElements]
public class AssemblyModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("condominiumId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = null!;

    [BsonElement("createdBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CreatedBy { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("description")]
    public string Description { get; set; } = null!;

    [BsonElement("startTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartTime { get; set; }

    [BsonElement("endTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndTime { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "agendada"; // agendada, em_andamento, encerrada

    [BsonElement("topics")]
    public List<TopicModel> Topics { get; set; } = new();

    [BsonElement("minutes")]
    public string? Minutes { get; set; }

    [BsonElement("participants")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> Participants { get; set; } = new();

    // Armazena resultados por tópico (índice do tópico -> mapa de opção -> percentual)
    [BsonElement("results")]
    public Dictionary<int, Dictionary<string, double>> Results { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("deletedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeletedAt { get; set; }

    [BsonElement("deletedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? DeletedBy { get; set; }

    [BsonIgnore]
    public DateTime ScheduledDate
    {
        get => StartTime;
        set
        {
            var normalized = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            StartTime = normalized.Date;
            EndTime = normalized.Date.AddDays(1).AddSeconds(-1);
        }
    }

    [BsonIgnore]
    public bool VoteStarted => NormalizeUtc(StartTime) <= DateTime.UtcNow;

    [BsonIgnore]
    public bool VoteFinished => NormalizeUtc(EndTime) < DateTime.UtcNow;

    [BsonIgnore]
    public string VoteStatus => !VoteStarted ? "Ainda não iniciou" : VoteFinished ? "Encerrada" : "Em andamento";

    [BsonIgnore]
    public string StartDate => NormalizeUtc(StartTime).ToString("dd/MM/yyyy");

    [BsonIgnore]
    public string EndDate => NormalizeUtc(EndTime).ToString("dd/MM/yyyy");

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

public class TopicModel
{
    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("desc")]
    public string? Desc { get; set; }

    [BsonElement("voteOptions")]
    public List<string> VoteOptions { get; set; } = new();
}
