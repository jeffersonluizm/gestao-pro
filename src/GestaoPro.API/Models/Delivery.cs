using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.Models;

public class Delivery
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("trackingCode")]
    public string TrackingCode { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "encomenda";

    [Required]
    [BsonElement("residentId")]
    public string ResidentId { get; set; } = string.Empty;

    [BsonElement("unitId")]
    public string UnitId { get; set; } = string.Empty;

    // Gravado pela API no cadastro (condomínio do token). Encomendas antigas não têm o campo:
    // o controle de acesso usa o condomínio do cadastro do morador.
    [BsonElement("condominiumId")]
    [BsonIgnoreIfNull]
    public string? CondominiumId { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    [BsonElement("arrivedAt")]
    public DateTime ArrivedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("pickedUpAt")]
    public DateTime? PickedUpAt { get; set; }

    [BsonElement("pickedUpBy")]
    public string? PickedUpBy { get; set; }

    [BsonElement("observations")]
    public string? Observations { get; set; }

    [BsonElement("qrCodeBase64")]
    public string? QrCodeBase64 { get; set; }

    [BsonElement("fotoUrl")]
    public string? FotoUrl { get; set; }
}
