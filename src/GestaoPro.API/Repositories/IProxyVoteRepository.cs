using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IProxyVoteRepository
{
    Task<ProxyVoteModel> CreateAsync(ProxyVoteModel proxy, CancellationToken ct = default);
    Task<ProxyVoteModel?> GetByAssemblyAndDelegatorAsync(string assemblyId, string delegatorUserId, CancellationToken ct = default);
    Task<List<ProxyVoteModel>> GetByAssemblyAsync(string assemblyId, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}