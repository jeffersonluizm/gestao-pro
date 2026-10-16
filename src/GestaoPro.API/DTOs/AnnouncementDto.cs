using System.ComponentModel.DataAnnotations;
namespace GestaoPro.API.DTOs;

public class CreateAnnouncementDto
{
    [StringLength(100)]
    public string CondominiumId { get; set; } = string.Empty;
    [StringLength(100)]
    public string AuthorId { get; set; } = string.Empty;
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateAnnouncementDto
{
    [StringLength(200)]
    public string? Title { get; set; }
    [StringLength(4000)]
    public string? Content { get; set; }
    [StringLength(50)]
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

