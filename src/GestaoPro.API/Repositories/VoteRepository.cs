using GestaoPro.API.Models;
using GestaoPro.API.Services;
using MongoDB.Driver;

namespace GestaoPro.API.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly IMongoCollection<VoteModel> _collection;

    public VoteRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<VoteModel>("votes");
    }

    public async Task<VoteModel> CreateAsync(VoteModel vote, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vote.Vote))
            throw new VoteOperationException("Voto inválido.", 400);

        // Verificação extra para evitar tentativas desnecessárias (opcional, mas boa prática)
        var alreadyVoted = await HasUserVotedAsync(vote.AssemblyId, vote.UserId, vote.TopicIndex, cancellationToken);
        if (alreadyVoted)
            throw new VoteOperationException("Você já votou nesta pauta.", 409);

        try
        {
            await _collection.InsertOneAsync(vote, null, cancellationToken);
            return vote;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new VoteOperationException("Você já votou nesta pauta.", 409);
        }
    }

    public async Task<VoteModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return await _collection.Find(v => v.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<VoteModel>> GetByAssemblyAsync(string assemblyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assemblyId))
            return new List<VoteModel>();

        return await _collection.Find(v => v.AssemblyId == assemblyId).ToListAsync(cancellationToken);
    }

    public async Task<VoteModel?> GetUserVoteAsync(string assemblyId, string userId, int topicIndex, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(v => v.AssemblyId == assemblyId && v.UserId == userId && v.TopicIndex == topicIndex)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(string id, VoteModel vote, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new VoteOperationException("A pauta informada não existe.", 400);

        if (string.IsNullOrWhiteSpace(vote.Vote))
            throw new VoteOperationException("Voto inválido.", 400);

        vote.Id = id; // Garante que o ID corresponda ao documento existente
        await _collection.ReplaceOneAsync(v => v.Id == id, vote, cancellationToken: cancellationToken);
    }

    public async Task<bool> HasUserVotedAsync(string assemblyId, string userId, int topicIndex, CancellationToken cancellationToken = default)
    {
        var count = await _collection
            .CountDocumentsAsync(
                v => v.AssemblyId == assemblyId && v.UserId == userId && v.TopicIndex == topicIndex,
                cancellationToken: cancellationToken);
        return count > 0;
    }
}
