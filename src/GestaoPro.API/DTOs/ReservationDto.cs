namespace GestaoPro.API.DTOs;

public class CreateReservationDto
{
    public string CommonAreaId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CondominiumId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? Observations { get; set; }
}

public class UpdateReservationDto
{
    public string? Date { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Status { get; set; }
    public string? Observations { get; set; }
}

