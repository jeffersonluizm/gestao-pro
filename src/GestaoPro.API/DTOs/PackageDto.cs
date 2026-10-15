using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public class CreatePackageDto
{
	[StringLength(100)]
	public string TrackingCode { get; set; } = string.Empty;
	[StringLength(50)]
	public string Carrier { get; set; } = string.Empty;
	[StringLength(100)]
	public string RecipientId { get; set; } = string.Empty;
	[StringLength(200)]
	public string RecipientName { get; set; } = string.Empty;
	[StringLength(200)]
	public string UnitNumber { get; set; } = string.Empty;
	[StringLength(100)]
	public string CondominiumId { get; set; } = string.Empty; // virÃ¡ do header
	[StringLength(4000)]
	public string Observations { get; set; } = string.Empty;
}

public class UpdatePackageDto
{
	[StringLength(100)]
	public string? TrackingCode { get; set; }
	[StringLength(50)]
	public string? Carrier { get; set; }
	[StringLength(100)]
	public string? RecipientId { get; set; }
	[StringLength(200)]
	public string? RecipientName { get; set; }
	[StringLength(200)]
	public string? UnitNumber { get; set; }
	[StringLength(100)]
	public string? CondominiumId { get; set; }
	[StringLength(50)]
	public string? Status { get; set; }
	[StringLength(4000)]
	public string? Observations { get; set; }
}
