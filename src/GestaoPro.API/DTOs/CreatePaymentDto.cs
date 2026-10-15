using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public record CreatePaymentDto(
    [StringLength(100)] string BillId,
    decimal AmountPaid,
    [StringLength(50)] string PaymentMethod, // Ex: "pix", "boleto", "cartao"
    [StringLength(500)] string? ReceiptUrl     // Opcional: link para o comprovante
);

public record UpdatePaymentDto(
    [StringLength(100)] string? BillId,
    DateTime? PaymentDate,
    decimal? AmountPaid,
    [StringLength(50)] string? PaymentMethod,
    [StringLength(500)] string? ReceiptUrl
);
