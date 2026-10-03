namespace GestaoPro.API.DTOs;

public class CreateAnnouncementDto
{
    public string CondominiumId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateAnnouncementDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

