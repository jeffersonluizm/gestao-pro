using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public class CreateAssemblyDto
{
    [StringLength(100)]
    public string CondominiumId { get; set; } = string.Empty;
    [StringLength(200)]
    public string CreatedBy { get; set; } = string.Empty; // Autor da assembleia
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime ScheduledDate { get; set; }
    [StringLength(4000)]
    public string? TopicsText { get; set; }

    // Lista de tÃ³picos para criaÃ§Ã£o inicial
    [MaxLength(30)]
    public List<CreateTopicDto> Topics { get; set; } = new();
}

public class UpdateAssemblyDto
{
    [StringLength(200)]
    public string? Title { get; set; }
    [StringLength(4000)]
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    [StringLength(50)]
    public string? Status { get; set; } // agendada, en_andamento, encerrada
    [StringLength(4000)]
    public string? Minutes { get; set; } // Campo 'ata' na imagem
    [StringLength(4000)]
    public string? TopicsText { get; set; }
    [MaxLength(30)]
    public List<CreateTopicDto>? Topics { get; set; }
}

// DTO Auxiliar para os tÃ³picos
public class CreateTopicDto
{
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(4000)]
    public string Desc { get; set; } = string.Empty;
    [MaxLength(20)]
    public List<string> VoteOptions { get; set; } = new();
}

