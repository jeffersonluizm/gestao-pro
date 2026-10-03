using GestaoPro.API.Models;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreateUnitRequest(
    [Required] string CondominiumId,
    [Required] string Number,
    string? Block,
    int? Floor,
    [Required] UnitType Type
);

public record UpdateUnitRequest(
    string? Number,
    string? Block,
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

