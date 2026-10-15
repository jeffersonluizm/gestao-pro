using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public record CreateBillDto(
    [StringLength(100)] string CondominiumId,
    [StringLength(100)] string UserId,
    [StringLength(4000)] string Description,
    decimal Amount,
    DateTime DueDate,
    [StringLength(100)] string? Barcode = null
);

public record UpdateBillDto(
    [StringLength(100)] string? CondominiumId,
    [StringLength(100)] string? UserId,
    [StringLength(4000)] string? Description,
    decimal? Amount,
    DateTime? DueDate,
    [StringLength(100)] string? Barcode,
    [StringLength(50)] string? Status
);
