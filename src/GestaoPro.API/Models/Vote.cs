using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

[BsonIgnoreExtraElements]
public class VoteModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("assemblyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AssemblyId { get; set; } = null!;

    [BsonElement("topicIndex")]
    public int TopicIndex { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("vote")]
    public string Vote { get; set; } = null!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


[BsonElement("votedByProxy")]
[BsonRepresentation(BsonType.ObjectId)]
public string? VotedByProxy { get; set; }

}