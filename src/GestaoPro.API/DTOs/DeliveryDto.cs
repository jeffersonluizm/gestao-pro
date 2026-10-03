namespace GestaoPro.API.DTOs;

public class UpdateDeliveryDto
{
    public string? TrackingCode { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? ResidentId { get; set; }
    public string? UnitId { get; set; }
    public string? Status { get; set; }
    public string? PickedUpBy { get; set; }
    public string? Observations { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? FotoUrl { get; set; }
}
