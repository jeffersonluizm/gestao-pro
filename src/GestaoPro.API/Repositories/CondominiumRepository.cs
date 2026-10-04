using MongoDB.Driver;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace GestaoPro.API.Repositories;

public class CondominiumRepository
{
    private readonly IMongoCollection<Condominium> _condominiums;

    public CondominiumRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _condominiums = database.GetCollection<Condominium>("condominiums");

        // Índice único para CNPJ
        _condominiums.Indexes.CreateOne(new CreateIndexModel<Condominium>(
            Builders<Condominium>.IndexKeys.Ascending(c => c.Cnpj),
            new CreateIndexOptions { Unique = true }));
    }


    // Busca todos os condomínios.
    public async Task<List<Condominium>> GetAllAsync() =>
        await _condominiums.Find(_ => true).ToListAsync();

    // Id e nome de todos, ordenados pelo nome (lista pública da tela de cadastro).
    public async Task<List<CondominiumSummary>> GetSummariesAsync() =>
        await _condominiums.Find(_ => true)
            .SortBy(c => c.Name)
            .Project(c => new CondominiumSummary(c.Id!, c.Name))
            .ToListAsync();

    // Busca um condomínio específico pelo _id do MongoDB.
    public async Task<Condominium?> GetByIdAsync(string id) =>
        ObjectId.TryParse(id, out _)
            ? await _condominiums.Find(c => c.Id == id).FirstOrDefaultAsync()
            : null;

    // Busca um condomínio pelo CNPJ.
    public async Task<Condominium?> GetByCnpjAsync(string cnpj) =>
        await _condominiums.Find(c => c.Cnpj == cnpj).FirstOrDefaultAsync();

    // Insere um novo condomínio na coleção.
    public async Task<Condominium> CreateAsync(Condominium condominium)
    {
        await _condominiums.InsertOneAsync(condominium);
        return condominium;
    }

    // Substitui o documento inteiro pelo novo. Atualiza o UpdatedAt automaticamente antes de salvar.
    public async Task UpdateAsync(string id, Condominium condominium)
    {
        condominium.UpdatedAt = DateTime.UtcNow;
        await _condominiums.ReplaceOneAsync(c => c.Id == id, condominium);
    }

    // Deleta o condomínio permanentemente.
    public async Task DeleteAsync(string id) =>
        await _condominiums.DeleteOneAsync(c => c.Id == id);
}

