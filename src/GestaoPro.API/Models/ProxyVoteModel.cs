using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

[BsonIgnoreExtraElements]
public class ProxyVoteModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("assemblyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AssemblyId { get; set; } = null!;

    [BsonElement("delegatorUserId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DelegatorUserId { get; set; } = null!; // Quem delega

    [BsonElement("delegateUserId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DelegateUserId { get; set; } = null!; // Quem recebe o voto

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } // Deve ser <= endTime da assembleia
}