using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public class CreateVisitorDto
{
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;
    [StringLength(200)]
    public string Document { get; set; } = string.Empty;
    [StringLength(40)]
    public string Phone { get; set; } = string.Empty;
    [StringLength(100)]
    public string ResidentId { get; set; } = string.Empty;
    [StringLength(100)]
    public string? CondominiumId { get; set; }
    [StringLength(100)]
    public string UnitId { get; set; } = string.Empty;
    [StringLength(4000)]
    public string Reason { get; set; } = string.Empty;
}

public class UpdateVisitorDto
{
    [StringLength(200)]
    public string? FullName { get; set; }
    [StringLength(200)]
    public string? Document { get; set; }
    [StringLength(40)]
    public string? Phone { get; set; }
    [StringLength(100)]
    public string? ResidentId { get; set; }
    [StringLength(100)]
    public string? CondominiumId { get; set; }
    [StringLength(100)]
    public string? UnitId { get; set; }
    [StringLength(4000)]
    public string? Reason { get; set; }
    [StringLength(50)]
    public string? Status { get; set; }
}
