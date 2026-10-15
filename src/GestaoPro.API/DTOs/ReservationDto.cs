using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public class CreateReservationDto
{
    [StringLength(100)]
    public string CommonAreaId { get; set; } = string.Empty;
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;
    [StringLength(100)]
    public string CondominiumId { get; set; } = string.Empty;
    [StringLength(40)]
    public string Date { get; set; } = string.Empty;
    [StringLength(40)]
    public string StartTime { get; set; } = string.Empty;
    [StringLength(40)]
    public string EndTime { get; set; } = string.Empty;
    [StringLength(4000)]
    public string? Observations { get; set; }
}

public class UpdateReservationDto
{
    [StringLength(40)]
    public string? Date { get; set; }
    [StringLength(40)]
    public string? StartTime { get; set; }
    [StringLength(40)]
    public string? EndTime { get; set; }
    [StringLength(50)]
    public string? Status { get; set; }
    [StringLength(4000)]
    public string? Observations { get; set; }
}

