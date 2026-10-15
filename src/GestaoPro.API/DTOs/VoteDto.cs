using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public class CreateVoteDto
{
    [StringLength(100)]
    public string AssemblyId { get; set; } = string.Empty;
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;
    public int TopicIndex { get; set; } = 0;
    [StringLength(50)]
    public string Vote { get; set; } = string.Empty; // Opção escolhida
}

// Geralmente votos nÃ£o sÃ£o editados para manter a integridade, 
// mas caso precise de uma alteraÃ§Ã£o administrativa:
public class UpdateVoteDto
{
    [StringLength(50)]
    public string? Vote { get; set; }
}

