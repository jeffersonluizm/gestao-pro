using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;
public interface IVoteService
{
    // Registra um novo voto no banco
    Task<VoteModel> CreateAsync(CreateVoteDto dto);

    Task<VoteModel?> GetByIdAsync(string id);

    // Busca todos os votos de uma assembleia especÃ­fica
    Task<List<VoteModel>> GetByAssemblyAsync(string assemblyId);

    Task<VoteModel?> UpdateAsync(string id, UpdateVoteDto dto);

    // Verifica se um usuÃ¡rio especÃ­fico jÃ¡ votou em um tÃ³pico (evita duplicidade)
    Task<bool> HasUserVotedAsync(string assemblyId, string userId, int topicIndex);
}

