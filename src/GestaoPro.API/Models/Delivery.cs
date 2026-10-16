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
    [StringLength(100)]
    public string TrackingCode { get; set; } = string.Empty;

    [BsonElement("description")]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [BsonElement("type")]
    [StringLength(50)]
    public string Type { get; set; } = "encomenda";

    [Required]
    [BsonElement("residentId")]
    [StringLength(100)]
    public string ResidentId { get; set; } = string.Empty;

    [BsonElement("unitId")]
    [StringLength(100)]
    public string UnitId { get; set; } = string.Empty;

    // Gravado pela API no cadastro (condomínio do token). Encomendas antigas não têm o campo:
    // o controle de acesso usa o condomínio do cadastro do morador.
    [BsonElement("condominiumId")]
    [BsonIgnoreIfNull]
    [StringLength(100)]
    public string? CondominiumId { get; set; }

    [BsonElement("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";

    [BsonElement("arrivedAt")]
    public DateTime ArrivedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("pickedUpAt")]
    public DateTime? PickedUpAt { get; set; }

    [BsonElement("pickedUpBy")]
    [StringLength(200)]
    public string? PickedUpBy { get; set; }

    [BsonElement("observations")]
    [StringLength(4000)]
    public string? Observations { get; set; }

    [BsonElement("qrCodeBase64")]
    [StringLength(20000)]
    public string? QrCodeBase64 { get; set; }

    [BsonElement("fotoUrl")]
    [StringLength(500)]
    public string? FotoUrl { get; set; }
}
