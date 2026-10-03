using MongoDB.Driver;
using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Repositories;

public class CredentialRepository
{
    private readonly IMongoCollection<Credential> _credentials;

    public CredentialRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _credentials = database.GetCollection<Credential>("credentials");

        // Índices únicos
        var uniqueIndex = new CreateIndexOptions { Unique = true };
        _credentials.Indexes.CreateOne(new CreateIndexModel<Credential>(
            Builders<Credential>.IndexKeys.Ascending(c => c.UserId), uniqueIndex));
        _credentials.Indexes.CreateOne(new CreateIndexModel<Credential>(
            Builders<Credential>.IndexKeys.Ascending(c => c.RegistrationNumber), uniqueIndex));
    }

    public async Task<Credential?> GetByRegistrationNumberAsync(string registrationNumber) =>
        await _credentials.Find(c => c.RegistrationNumber == registrationNumber).FirstOrDefaultAsync();

    public async Task<Credential?> GetByUserIdAsync(string userId) =>
        await _credentials.Find(c => c.UserId == userId).FirstOrDefaultAsync();

    public async Task<bool> HasAnyAdministratorAsync() =>
        await _credentials.Find(c =>
            c.IsActive &&
            (c.Role == "admin" || c.Role == "sindico")).AnyAsync();

    public async Task<Credential> CreateAsync(Credential credential)
    {
        await _credentials.InsertOneAsync(credential);
        return credential;
    }

    public async Task UpdatePasswordAsync(string userId, string newPasswordHash, bool mustChangePassword)
    {
        var update = Builders<Credential>.Update
            .Set(c => c.PasswordHash, newPasswordHash)
            .Set(c => c.MustChangePassword, mustChangePassword)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        await _credentials.UpdateOneAsync(c => c.UserId == userId, update);
    }

    public async Task DeactivateAsync(string userId)
    {
        var update = Builders<Credential>.Update
            .Set(c => c.IsActive, false)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        await _credentials.UpdateOneAsync(c => c.UserId == userId, update);
    }
}

