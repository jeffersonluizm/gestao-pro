using GestaoPro.API.Models;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreateUnitRequest(
    [StringLength(100)][Required] string CondominiumId,
    [StringLength(40)][Required] string Number,
    [StringLength(40)] string? Block,
    int? Floor,
    [Required] UnitType Type
);

public record UpdateUnitRequest(
    [StringLength(40)] string? Number,
    [StringLength(40)] string? Block,
    int? Floor,
    UnitType? Type
);

public record UnitResponse(
    string Id,
    string CondominiumId,
    string Number,
    string? Block,
    int? Floor,
    UnitType Type,
    List<string> ResidentIds,
    int ResidentCount,
    DateTime? LastResidentUpdateAt,
    ResidentUpdateType? LastResidentUpdateType,
    DateTime CreatedAt
);

