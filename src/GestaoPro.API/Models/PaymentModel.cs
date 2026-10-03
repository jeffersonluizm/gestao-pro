using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GestaoPro.API.Models;

public class PaymentModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string BillId { get; set; } = null!; // Relacionamento com o Boleto acima

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    
    public decimal AmountPaid { get; set; } // Valor efetivamente pago
    
    // MÃ©todo: "pix", "credit_card", "bank_transfer"
    public string PaymentMethod { get; set; } = "pix";
    
    public string? ReceiptUrl { get; set; } // URL para o comprovante (opcional)
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

