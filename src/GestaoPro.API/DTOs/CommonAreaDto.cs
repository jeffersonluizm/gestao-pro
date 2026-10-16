using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public class CreateCommonAreaDto
{
    [Required]
    [StringLength(100)]
    public string CondominiumId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    [Required]
    [StringLength(40)]
    public string OpenTime { get; set; } = "08:00";

    [Required]
    [StringLength(40)]
    public string CloseTime { get; set; } = "22:00";

    [StringLength(4000)]
    public string Rules { get; set; } = string.Empty;
}

public class UpdateCommonAreaDto
{
    [StringLength(200)]
    public string? Name { get; set; }
    [StringLength(4000)]
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    [StringLength(40)]
    public string? OpenTime { get; set; }
    [StringLength(40)]
    public string? CloseTime { get; set; }
    [StringLength(4000)]
    public string? Rules { get; set; }
    public bool? IsActive { get; set; }
}
