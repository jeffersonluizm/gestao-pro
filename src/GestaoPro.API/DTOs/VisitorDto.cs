namespace GestaoPro.API.DTOs;

public class CreateVisitorDto
{
    public string FullName { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ResidentId { get; set; } = string.Empty;
    public string? CondominiumId { get; set; }
    public string UnitId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class UpdateVisitorDto
{
    public string? FullName { get; set; }
    public string? Document { get; set; }
    public string? Phone { get; set; }
    public string? ResidentId { get; set; }
    public string? CondominiumId { get; set; }
    public string? UnitId { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
}
