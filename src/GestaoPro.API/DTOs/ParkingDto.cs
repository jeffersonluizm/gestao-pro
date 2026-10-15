using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public class CreateParkingDto
{
    [StringLength(40)]
    public string Number { get; set; } = string.Empty;
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
    [StringLength(100)]
    public string? CondominiumId { get; set; }
    public bool IsShared { get; set; }
    [StringLength(100)]
    public string? UnitId { get; set; }
}

public class UpdateParkingDto
{
    [StringLength(40)]
    public string? Number { get; set; }
    [StringLength(50)]
    public string? Type { get; set; }
    [StringLength(100)]
    public string? CondominiumId { get; set; }
    [StringLength(50)]
    public string? Status { get; set; }
    public bool? IsShared { get; set; }
    [StringLength(100)]
    public string? ResidentId { get; set; }
    [StringLength(100)]
    public string? UnitId { get; set; }
    [StringLength(100)]
    public string? ReservedByUserId { get; set; }
}
