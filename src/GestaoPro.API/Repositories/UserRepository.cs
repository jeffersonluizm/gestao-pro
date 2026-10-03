using GestaoPro.API.Models;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace GestaoPro.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<BsonDocument> _userDocuments;
    private readonly IMongoCollection<BsonDocument> _legacyUsers;

    public UserRepository(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _users = database.GetCollection<User>("users");
        _userDocuments = database.GetCollection<BsonDocument>("users");
        _legacyUsers = database.GetCollection<BsonDocument>("user");

        var uniqueIndex = new CreateIndexOptions { Unique = true };
        _users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email), uniqueIndex));
        _users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.RegistrationNumber), uniqueIndex));

        _users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys
                .Ascending(u => u.CondominiumId)
                .Ascending(u => u.Role)));
    }

    public async Task<List<User>> GetAllInactiveAsync() =>
        await _users.Find(u => !u.IsActive).ToListAsync();

    public async Task<List<User>> GetAllIncludingInactiveAsync() =>
        await _users.Find(_ => true).ToListAsync();

    public async Task<List<User>> GetAllActiveAsync() =>
        await _users.Find(u => u.IsActive).ToListAsync();

    public async Task<bool> HasAnyActiveAsync() =>
        await _users.Find(u => u.IsActive).AnyAsync();

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

    public async Task<User?> GetByCpfAsync(string cpf) =>
        await _users.Find(u => u.Cpf == cpf).FirstOrDefaultAsync();

    public async Task<User?> GetByRegistrationNumberAsync(string registrationNumber) =>
        await _users.Find(u => u.RegistrationNumber == registrationNumber).FirstOrDefaultAsync();

    public async Task<User?> GetByRegistrationNumberIncludingLegacyAsync(string registrationNumber)
    {
        var user = await GetByRegistrationNumberAsync(registrationNumber);
        if (user is not null)
            return user;

        var document =
            await FindLegacyUserByRegistrationNumberAsync(_userDocuments, registrationNumber) ??
            await FindLegacyUserByRegistrationNumberAsync(_legacyUsers, registrationNumber);

        return document is null ? null : MapLegacyUser(document);
    }

    public async Task<List<User>> GetByCondominiumAsync(string condominiumId) =>
        await _users.Find(u => u.CondominiumId == condominiumId && u.IsActive).ToListAsync();

    public async Task<List<User>> GetByCondominiumIncludingInactiveAsync(string condominiumId) =>
        await _users.Find(u => u.CondominiumId == condominiumId).ToListAsync();

    public async Task<User> CreateAsync(User user)
    {
        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task UpdateAsync(string id, User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await _users.ReplaceOneAsync(u => u.Id == id, user);
    }

    public async Task DeactivateAsync(string id)
    {
        var update = Builders<User>.Update
            .Set(u => u.IsActive, false)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await _users.UpdateOneAsync(u => u.Id == id, update);
    }

    public async Task UpdatePasswordAsync(string id, string newPasswordHash)
    {
        var update = Builders<User>.Update
            .Set(u => u.PasswordHash, newPasswordHash)
            .Set(u => u.MustChangePassword, false)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await _users.UpdateOneAsync(u => u.Id == id, update);
    }

    private static async Task<BsonDocument?> FindLegacyUserByRegistrationNumberAsync(
        IMongoCollection<BsonDocument> collection,
        string registrationNumber)
    {
        var value = CleanLegacyValue(registrationNumber);
        var regex = new BsonRegularExpression($"^\\s*{Regex.Escape(value)}\\s*,?\\s*$", "i");
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Regex("RegistrationNumber", regex),
            Builders<BsonDocument>.Filter.Regex("registrationNumber", regex),
            Builders<BsonDocument>.Filter.Regex("matricula", regex),
            Builders<BsonDocument>.Filter.Regex("matrícula", regex)
        );

        return await collection.Find(filter).FirstOrDefaultAsync();
    }

    private static User MapLegacyUser(BsonDocument document)
    {
        var role = CleanLegacyValue(GetString(document, "role"));
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            parsedRole = UserRole.morador;

        return new User
        {
            Id = GetDocumentId(document),
            CondominiumId = GetFirstString(document, "CondominiumId", "condominiumId", "condominioId"),
            UnitId = EmptyToNull(GetFirstString(document, "UnitId", "unitId", "unidadeId")),
            Name = GetFirstString(document, "Name", "name", "nome"),
            Email = GetFirstString(document, "Email", "email"),
            RegistrationNumber = GetFirstString(document, "RegistrationNumber", "registrationNumber", "matricula", "matrícula"),
            PasswordHash = GetFirstString(document, "PasswordHash", "passwordHash", "senhaHash", "senha"),
            Phone = GetFirstString(document, "Phone", "phone", "telefone"),
            Cpf = GetFirstString(document, "Cpf", "CPF", "cpf"),
            Role = parsedRole,
            IsActive = GetBoolean(document, true, "IsActive", "isActive", "ativo"),
            MustChangePassword = GetBoolean(document, false, "MustChangePassword", "mustChangePassword")
        };
    }

    private static string GetFirstString(BsonDocument document, params string[] names)
    {
        foreach (var name in names)
        {
            var value = GetString(document, name);
            if (!string.IsNullOrWhiteSpace(value))
                return CleanLegacyValue(value);
        }

        return string.Empty;
    }

    private static string GetString(BsonDocument document, string name)
    {
        if (!document.TryGetValue(name, out var value) || value.IsBsonNull)
            return string.Empty;

        return value.ToString() ?? string.Empty;
    }

    private static string GetDocumentId(BsonDocument document)
    {
        if (!document.TryGetValue("_id", out var value) || value.IsBsonNull)
            return string.Empty;

        return value.IsObjectId
            ? value.AsObjectId.ToString()
            : CleanLegacyValue(value.ToString() ?? string.Empty);
    }

    private static bool GetBoolean(BsonDocument document, bool defaultValue, params string[] names)
    {
        foreach (var name in names)
        {
            if (!document.TryGetValue(name, out var value) || value.IsBsonNull)
                continue;

            if (value.IsBoolean)
                return value.AsBoolean;

            if (bool.TryParse(CleanLegacyValue(value.ToString() ?? string.Empty), out var parsed))
                return parsed;
        }

        return defaultValue;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string CleanLegacyValue(string value) =>
        value.Trim().Trim(',', '/', '\\').Trim();
}
