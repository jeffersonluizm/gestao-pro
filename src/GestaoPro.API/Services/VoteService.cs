using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class VoteService : IVoteService
{
    private readonly IVoteRepository _voteRepository;
    private readonly IAssemblyRepository _assemblyRepository;
    private readonly IProxyVoteRepository _proxyRepository;
    private readonly IUserRepository _userRepository; // assumindo existente
    private readonly TimeProvider _timeProvider;

    public VoteService(IVoteRepository voteRepo, IAssemblyRepository assemblyRepo,
        IProxyVoteRepository proxyRepo, IUserRepository userRepo, TimeProvider? timeProvider = null)
    {
        _voteRepository = voteRepo;
        _assemblyRepository = assemblyRepo;
        _proxyRepository = proxyRepo;
        _userRepository = userRepo;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<VoteModel> CreateAsync(CreateVoteDto dto)
    {
        try
        {
            return await RegisterVoteAsync(dto.AssemblyId, dto.UserId, dto.Vote, dto.TopicIndex);
        }
        catch (VoteOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new VoteOperationException("Ocorreu um erro interno ao processar a votação.", 500);
        }
    }

    public async Task<VoteModel?> GetByIdAsync(string id) =>
        await _voteRepository.GetByIdAsync(id);

    public async Task<List<VoteModel>> GetByAssemblyAsync(string assemblyId) =>
        await _voteRepository.GetByAssemblyAsync(assemblyId);

    public async Task<VoteModel?> UpdateAsync(string id, UpdateVoteDto dto)
    {
        var existingVote = await _voteRepository.GetByIdAsync(id);
        if (existingVote is null || string.IsNullOrWhiteSpace(dto.Vote))
            return existingVote;

        existingVote.Vote = dto.Vote;
        await _voteRepository.UpdateAsync(id, existingVote);
        return existingVote;
    }

    public async Task<bool> HasUserVotedAsync(string assemblyId, string userId, int topicIndex) =>
        await _voteRepository.HasUserVotedAsync(assemblyId, userId, topicIndex);

    public async Task<VoteModel> RegisterVoteAsync(string assemblyId, string userId, string voteValue, int topicIndex = 0)
    {
        var assembly = await _assemblyRepository.GetByIdAsync(assemblyId);
        if (assembly == null)
            throw new VoteOperationException("A assembleia informada não foi encontrada.", 404);
        if (assembly.IsDeleted)
            throw new VoteOperationException("A assembleia informada foi removida e não aceita novos votos.", 400);

        if (assembly.Topics == null || assembly.Topics.Count == 0)
            throw new VoteOperationException("Esta assembleia não possui pautas disponíveis para votação.", 400);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new VoteOperationException("Usuário não encontrado.", 404);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var startDate = NormalizeUtc(assembly.StartTime);
        var endDate = NormalizeUtc(assembly.EndTime);

        if (now < startDate)
            throw new VoteOperationException("A votação ainda não foi iniciada.", 400);
        if (now > endDate)
            throw new VoteOperationException("A votação já foi encerrada.", 400);

        var normalizedStatus = NormalizeStatus(assembly.Status);
        if (normalizedStatus is "encerrada" or "cancelada")
            throw new VoteOperationException("A votação desta assembleia não está ativa no momento.", 400);

        if (topicIndex < 0 || topicIndex >= assembly.Topics.Count)
            throw new VoteOperationException("A pauta informada não existe.", 400);

        var topic = assembly.Topics[topicIndex];
        var availableOptions = NormalizeVoteOptions(topic.VoteOptions);
        if (!availableOptions.Contains(NormalizeVoteValue(voteValue)))
        {
            var optionsText = string.Join(", ", topic.VoteOptions.Where(option => !string.IsNullOrWhiteSpace(option)));
            throw new VoteOperationException($"Voto inválido. Utilize uma das opções cadastradas: {optionsText}.", 400);
        }

        // Verifica se o usuário já votou diretamente
        var hasVoted = await _voteRepository.HasUserVotedAsync(assemblyId, userId, topicIndex);
        if (hasVoted)
            throw new VoteOperationException("Você já votou nesta pauta.", 409);

        // Verifica se existe delegação deste usuário para outro
        var proxy = await _proxyRepository.GetByAssemblyAndDelegatorAsync(assemblyId, userId);
        if (proxy != null)
        {
            throw new VoteOperationException("Você não possui permissão para votar nesta assembleia.", 403);
        }

        // Se o usuário está atuando como representante (delegate), verificar se tem permissão
        var delegatesForThisUser = await _proxyRepository.GetByAssemblyAsync(assemblyId);
        var delegators = delegatesForThisUser.Where(p => p.DelegateUserId == userId).ToList();
        if (delegators.Any())
        {
            // Registra que o representante está votando EM NOME de cada delegador
            foreach (var d in delegators)
            {
                var alreadyVotedForDelegator = await _voteRepository.HasUserVotedAsync(assemblyId, d.DelegatorUserId, topicIndex);
                if (!alreadyVotedForDelegator)
                {
                    var proxyVote = new VoteModel
                    {
                        AssemblyId = assemblyId,
                        UserId = d.DelegatorUserId,
                        TopicIndex = topicIndex,
                        Vote = voteValue,
                        CreatedAt = DateTime.UtcNow,
                        VotedByProxy = userId // você pode adicionar este campo opcional no VoteModel
                    };
                    await _voteRepository.CreateAsync(proxyVote);
                }
            }
        }

        // Registra o voto do próprio usuário (caso não tenha delegado e não seja representante - mas representante vota em nome, não para si mesmo)
        var vote = new VoteModel
        {
            AssemblyId = assemblyId,
            UserId = userId,
            TopicIndex = topicIndex,
            Vote = voteValue,
            CreatedAt = DateTime.UtcNow
        };
        return await _voteRepository.CreateAsync(vote);
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return string.Empty;
        return status.Trim().ToLowerInvariant().Replace("en_", "em_");
    }

    private static HashSet<string> NormalizeVoteOptions(IEnumerable<string>? options)
    {
        var normalized = (options ?? Enumerable.Empty<string>())
            .Select(NormalizeVoteValue)
            .Where(option => option.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return normalized.Count > 0
            ? normalized
            : new HashSet<string>(new[] { "sim", "não" }, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeVoteValue(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "nao" ? "não" : normalized;
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

}
