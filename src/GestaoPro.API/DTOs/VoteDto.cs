using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public class CreateVoteDto
{
    public string AssemblyId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int TopicIndex { get; set; } = 0;
    public string Vote { get; set; } = string.Empty; // Opção escolhida
}

// Geralmente votos nÃ£o sÃ£o editados para manter a integridade, 
// mas caso precise de uma alteraÃ§Ã£o administrativa:
public class UpdateVoteDto
{
    public string? Vote { get; set; }
}

