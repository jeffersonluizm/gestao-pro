using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services.Demo;

public interface IDemoResidentStore
{
    Task<User?> GetByCpfAsync(string cpf);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(string id, User user);
}

public sealed class MongoDemoResidentStore : IDemoResidentStore
{
    private readonly UserRepository _users;

    public MongoDemoResidentStore(UserRepository users) => _users = users;

    public Task<User?> GetByCpfAsync(string cpf) => _users.GetByCpfAsync(cpf);
    public Task<User> CreateAsync(User user) => _users.CreateAsync(user);
    public Task UpdateAsync(string id, User user) => _users.UpdateAsync(id, user);
}

// Moradores pré-cadastrados sem acesso, para experimentar "Ative o seu acesso" com o código do condomínio.
// Roda depois da limpeza: só os usuários criados aqui viram registros semeados (por id). Um usuário que ainda tenha o
// CPF de exemplo foi gravado por outra pessoa (outro condomínio ou visitante): é ignorado com log, nunca adotado.
public sealed class DemoPreRegisteredResidents
{
    private readonly IDemoResidentStore _store;
    private readonly ILogger<DemoPreRegisteredResidents> _logger;

    public DemoPreRegisteredResidents(IDemoResidentStore store, ILogger<DemoPreRegisteredResidents> logger)
    {
        _store = store;
        _logger = logger;
    }

    // Devolve os ids dos pré-cadastrados do condomínio demo (protegidos contra remoção durante a demonstração).
    public async Task<IReadOnlyList<string>> EnsureAsync(string condominiumId)
    {
        var ids = new List<string>();
        foreach (var (cpf, name) in DemoCatalog.PreRegisteredResidents)
        {
            var existing = await _store.GetByCpfAsync(cpf);
            if (existing is not null)
            {
                _logger.LogWarning("Modo demonstração: o CPF de exemplo final {Final} já pertence a outro usuário e foi ignorado.", cpf[^2..]);
                continue;
            }

            var created = await _store.CreateAsync(new User
            {
                CondominiumId = condominiumId, Name = name, Email = $"{cpf}@pending.gestaopro.local", Phone = "3133330000",
                Cpf = cpf, RegistrationNumber = $"PENDING-{cpf}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role = UserRole.morador, MustChangePassword = true, IsActive = true,
            });
            if (created?.Id is not null) ids.Add(created.Id);
        }
        return ids;
    }
}
