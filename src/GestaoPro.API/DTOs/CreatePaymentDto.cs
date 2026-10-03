namespace GestaoPro.API.DTOs;

public record CreatePaymentDto(
    string BillId,
    decimal AmountPaid,
    string PaymentMethod, // Ex: "pix", "boleto", "cartao"
    string? ReceiptUrl     // Opcional: link para o comprovante
);

public record UpdatePaymentDto(
    string? BillId,
    DateTime? PaymentDate,
    decimal? AmountPaid,
    string? PaymentMethod,
    string? ReceiptUrl
);
