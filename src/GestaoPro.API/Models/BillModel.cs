using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

public class BillModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = null!; // ID do CondomÃ­nio

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // ID do Morador

    public string Description { get; set; } = null!; // Ex: "Taxa Condominial 05/2026"
    
    public decimal Amount { get; set; } // Valor do boleto
    
    public DateTime DueDate { get; set; } // Data de Vencimento
    
    // Status possÃ­veis: "pending", "paid", "overdue", "canceled"
    public string Status { get; set; } = "pending";

    public string Barcode { get; set; } = null!; // Linha digitÃ¡vel/CÃ³digo de barras
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

