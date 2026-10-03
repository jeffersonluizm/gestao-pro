using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

public enum UnitType
{
    apt,
    house
}

public enum ResidentUpdateType
{
    entrada,
    saida
}

public class Unit
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    // Block e Floor podem ser nulls pois type:house não necessáriamente precisa dessas propriedades.
    public string? Block { get; set; } = string.Empty;
    public int? Floor { get; set; }
    public UnitType Type { get; set; } = UnitType.apt;

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ResidentIds { get; set; } = new();

    // Ultima data de mudança de residentes
    public DateTime? LastResidentUpdateAt { get; set; }

    // Tipo da mudança
    public ResidentUpdateType? LastResidentUpdateType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

