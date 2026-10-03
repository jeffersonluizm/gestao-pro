using MongoDB.Driver;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Repositories;

public class UnitRepository
{
    private readonly IMongoCollection<Unit> _units;

    public UnitRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _units = database.GetCollection<Unit>("units");

        // Índice único composto — não pode ter duas unidades com mesmo número e bloco no mesmo condomínio
        _units.Indexes.CreateOne(new CreateIndexModel<Unit>(
            Builders<Unit>.IndexKeys
                .Ascending(u => u.CondominiumId)
                .Ascending(u => u.Number)
                .Ascending(u => u.Block),
            new CreateIndexOptions { Unique = true }));
    }


    // Retorna todas as unidades baseado no condominio.
    public async Task<List<Unit>> GetAllByCondominiumAsync(string condominiumId) =>
        await _units.Find(u => u.CondominiumId == condominiumId).ToListAsync();

    // Retorna uma unidade baseado no _id do MongoDB.
    public async Task<Unit?> GetByIdAsync(string id) =>
        await _units.Find(u => u.Id == id).FirstOrDefaultAsync();

    // Retorna uma unidade baseado no condominio, número da unidade e bloco, usar para verificar ser unidade já existe.
    public async Task<Unit?> GetByNumberAndBlockAsync(string condominiumId, string number, string? block) =>
        await _units.Find(u => u.CondominiumId == condominiumId &&
                               u.Number == number &&
                               u.Block == block).FirstOrDefaultAsync();

    public async Task<Unit?> FindByLabelAsync(string condominiumId, string label)
    {
        var normalizedLabel = NormalizeUnitLabel(label);
        var units = await GetAllByCondominiumAsync(condominiumId);
        return units.FirstOrDefault(unit =>
            NormalizeUnitLabel(unit.Number) == normalizedLabel ||
            NormalizeUnitLabel($"{unit.Block} {unit.Number}") == normalizedLabel ||
            NormalizeUnitLabel($"Apto {unit.Number}") == normalizedLabel ||
            NormalizeUnitLabel($"Apartamento {unit.Number}") == normalizedLabel ||
            NormalizeUnitLabel($"Casa {unit.Number}") == normalizedLabel);
    }

    // Insere novo documento na coleção units.
    public async Task<Unit> CreateAsync(Unit unit)
    {
        await _units.InsertOneAsync(unit);
        return unit;
    }

    // Substitui o documento inteiro pelo novo, atualizando o UpdatedAt automaticamente.
    public async Task UpdateAsync(string id, Unit unit) =>
        await _units.ReplaceOneAsync(u => u.Id == id, unit);

    // Adiciona um morador na unidade.
    public async Task AddResidentAsync(string id, string residentId)
    {
        var update = Builders<Unit>.Update
            .AddToSet(u => u.ResidentIds, residentId)
            .Set(u => u.LastResidentUpdateAt, DateTime.UtcNow)
            .Set(u => u.LastResidentUpdateType, ResidentUpdateType.entrada);
        await _units.UpdateOneAsync(u => u.Id == id, update);
    }

    // Remove um morador da unidade.
    public async Task RemoveResidentAsync(string id, string residentId)
    {
        var update = Builders<Unit>.Update
            .Pull(u => u.ResidentIds, residentId)
            .Set(u => u.LastResidentUpdateAt, DateTime.UtcNow)
            .Set(u => u.LastResidentUpdateType, ResidentUpdateType.saida);
        await _units.UpdateOneAsync(u => u.Id == id, update);
    }

    // Deleta a unidade permanentemente.
    public async Task DeleteAsync(string id) =>
        await _units.DeleteOneAsync(u => u.Id == id);

    private static string NormalizeUnitLabel(string value) =>
        new(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
}

