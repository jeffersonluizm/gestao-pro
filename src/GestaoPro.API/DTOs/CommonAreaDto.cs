using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public class CreateCommonAreaDto
{
    [Required]
    public string CondominiumId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    [Required]
    public string OpenTime { get; set; } = "08:00";

    [Required]
    public string CloseTime { get; set; } = "22:00";

    public string Rules { get; set; } = string.Empty;
}

public class UpdateCommonAreaDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public string? Rules { get; set; }
    public bool? IsActive { get; set; }
}
