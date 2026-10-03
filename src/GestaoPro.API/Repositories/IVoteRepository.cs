using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IVoteRepository
{
    /// <summary>
    /// Cria um novo voto. Lança exceção se o usuário já votou nesta pauta ou se o voto for inválido.
    /// </summary>
    Task<VoteModel> CreateAsync(VoteModel vote, CancellationToken cancellationToken = default);

    Task<VoteModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    
    Task<List<VoteModel>> GetByAssemblyAsync(string assemblyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtém o voto específico de um usuário em um tópico de uma assembleia.
    /// </summary>
    Task<VoteModel?> GetUserVoteAsync(string assemblyId, string userId, int topicIndex, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Atualiza um voto existente (muda de "sim" para "não" ou vice-versa).
    /// </summary>
    Task UpdateAsync(string id, VoteModel vote, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica se o usuário já votou na pauta informada.
    /// </summary>
    Task<bool> HasUserVotedAsync(string assemblyId, string userId, int topicIndex, CancellationToken cancellationToken = default);
}
