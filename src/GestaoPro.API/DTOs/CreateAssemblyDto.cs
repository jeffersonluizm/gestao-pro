using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public class CreateAssemblyDto
{
    public string CondominiumId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty; // Autor da assembleia
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? TopicsText { get; set; }

    // Lista de tÃ³picos para criaÃ§Ã£o inicial
    public List<CreateTopicDto> Topics { get; set; } = new();
}

public class UpdateAssemblyDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Status { get; set; } // agendada, en_andamento, encerrada
    public string? Minutes { get; set; } // Campo 'ata' na imagem
    public string? TopicsText { get; set; }
    public List<CreateTopicDto>? Topics { get; set; }
}

// DTO Auxiliar para os tÃ³picos
public class CreateTopicDto
{
    public string Title { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public List<string> VoteOptions { get; set; } = new();
}

