using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public class UpdateDeliveryDto
{
    [StringLength(100)]
    public string? TrackingCode { get; set; }
    [StringLength(4000)]
    public string? Description { get; set; }
    [StringLength(50)]
    public string? Type { get; set; }
    [StringLength(100)]
    public string? ResidentId { get; set; }
    [StringLength(100)]
    public string? UnitId { get; set; }
    [StringLength(50)]
    public string? Status { get; set; }
    [StringLength(200)]
    public string? PickedUpBy { get; set; }
    [StringLength(4000)]
    public string? Observations { get; set; }
    [StringLength(20000)]
    public string? QrCodeBase64 { get; set; }
    [StringLength(500)]
    public string? FotoUrl { get; set; }
}
