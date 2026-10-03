namespace GestaoPro.API.DTOs;

public record CreateBillDto(
    string CondominiumId,
    string UserId,
    string Description,
    decimal Amount,
    DateTime DueDate,
    string? Barcode = null
);

public record UpdateBillDto(
    string? CondominiumId,
    string? UserId,
    string? Description,
    decimal? Amount,
    DateTime? DueDate,
    string? Barcode,
    string? Status
);
