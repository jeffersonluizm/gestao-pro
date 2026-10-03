namespace GestaoPro.API.DTOs;

public class CreateParkingDto
{
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CondominiumId { get; set; }
    public bool IsShared { get; set; }
    public string? UnitId { get; set; }
}

public class UpdateParkingDto
{
    public string? Number { get; set; }
    public string? Type { get; set; }
    public string? CondominiumId { get; set; }
    public string? Status { get; set; }
    public bool? IsShared { get; set; }
    public string? ResidentId { get; set; }
    public string? UnitId { get; set; }
    public string? ReservedByUserId { get; set; }
}
