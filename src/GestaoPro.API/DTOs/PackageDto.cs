namespace GestaoPro.API.DTOs;

public class CreatePackageDto
{
	public string TrackingCode { get; set; } = string.Empty;
	public string Carrier { get; set; } = string.Empty;
	public string RecipientId { get; set; } = string.Empty;
	public string RecipientName { get; set; } = string.Empty;
	public string UnitNumber { get; set; } = string.Empty;
	public string CondominiumId { get; set; } = string.Empty; // virÃ¡ do header
	public string Observations { get; set; } = string.Empty;
}

public class UpdatePackageDto
{
	public string? TrackingCode { get; set; }
	public string? Carrier { get; set; }
	public string? RecipientId { get; set; }
	public string? RecipientName { get; set; }
	public string? UnitNumber { get; set; }
	public string? CondominiumId { get; set; }
	public string? Status { get; set; }
	public string? Observations { get; set; }
}
