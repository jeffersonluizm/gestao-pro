using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class DataSeeder
{
    private const string SeedPassword = "Admin@123";
    private const string SeedCpf = "00000000000";
    private const string SeedEmail = "admin@gestaopro.local";
    private const string SeedCnpj = "00.000.000/0001-00";

    private static readonly (string Cpf, string Name)[] MockResidents =
    [
        ("11111111111", "João da Silva (Teste)"),
        ("22222222222", "Maria Santos (Teste)"),
        ("33333333333", "Pedro Oliveira (Teste)"),
    ];

    private static readonly (string Name, string Description, int Capacity, string Open, string Close)[] MockCommonAreas =
    [
        ("Salão de Festas",   "Espaço para eventos e confraternizações",  80, "08:00", "23:00"),
        ("Academia",          "Equipamentos de musculação e cardio",       20, "06:00", "22:00"),
        ("Piscina",           "Piscina adulto e infantil",                 30, "08:00", "20:00"),
        ("Churrasqueira",     "Área gourmet com churrasqueira coberta",    15, "10:00", "22:00"),
        ("Quadra Poliesportiva", "Quadra de futebol, vôlei e basquete",    20, "07:00", "21:00"),
    ];

    private readonly UserRepository _users;
    private readonly CredentialRepository _credentials;
    private readonly CondominiumRepository _condominiums;
    private readonly ICommonAreaRepository _commonAreas;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        UserRepository users,
        CredentialRepository credentials,
        CondominiumRepository condominiums,
        ICommonAreaRepository commonAreas,
        ILogger<DataSeeder> logger)
    {
        _users = users;
        _credentials = credentials;
        _condominiums = condominiums;
        _commonAreas = commonAreas;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // ── Condomínio ────────────────────────────────────────────────────────
        var condominium = await _condominiums.GetByCnpjAsync(SeedCnpj)
            ?? await _condominiums.CreateAsync(new Condominium
            {
                Name = "Condomínio GestaoPro",
                Cnpj = SeedCnpj,
                Address = new Address
                {
                    Street = "Rua Exemplo",
                    Number = "1",
                    Neighborhood = "Centro",
                    City = "Belo Horizonte",
                    State = "MG",
                    ZipCode = "30000-000"
                },
                DefaultPassword = BCrypt.Net.BCrypt.HashPassword(SeedPassword)
            });

        // ── Áreas comuns ──────────────────────────────────────────────────────
        await SeedCommonAreasAsync(condominium.Id!);

        // ── Admin já existe? ──────────────────────────────────────────────────
        var existingUser = await _users.GetByCpfAsync(SeedCpf);
        if (existingUser is not null)
        {
            var existingCred = await _credentials.GetByUserIdAsync(existingUser.Id!);
            if (existingCred is not null)
            {
                // Sempre reseta a senha do admin de teste para garantir acesso consistente.
                // Isso evita que a conta fique inacessível após alterações de senha.
                var resetHash = BCrypt.Net.BCrypt.HashPassword(SeedPassword);
                await _users.UpdatePasswordAsync(existingUser.Id!, resetHash);
                await _credentials.UpdatePasswordAsync(existingUser.Id!, resetHash, false);
                _logger.LogInformation("=== ADMIN DE TESTE ===");
                _logger.LogInformation("E-mail: {Email} | Senha: {Pwd} (resetada)", SeedEmail, SeedPassword);
                _logger.LogInformation("=====================");
                await SeedMockResidentsAsync(condominium.Id!);
                return;
            }
        }

        // ── Cria admin ────────────────────────────────────────────────────────
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(SeedPassword);

        var user = existingUser ?? await _users.CreateAsync(new User
        {
            CondominiumId = condominium.Id!,
            Name = "Administrador GestaoPro",
            Email = SeedEmail,
            Phone = "00000000000",
            Cpf = SeedCpf,
            RegistrationNumber = SeedEmail,
            PasswordHash = passwordHash,
            Role = UserRole.admin,
            MustChangePassword = false,
            IsActive = true
        });

        await SeedMockResidentsAsync(condominium.Id!);

        try
        {
            await _credentials.CreateAsync(new Credential
            {
                UserId = user.Id!,
                RegistrationNumber = SeedEmail,
                PasswordHash = passwordHash,
                Role = "admin",
                CondominiumId = condominium.Id!,
                MustChangePassword = false,
                IsActive = true
            });

            _logger.LogInformation("=== ADMIN DE TESTE CRIADO ===");
            _logger.LogInformation("E-mail : {Email}", SeedEmail);
            _logger.LogInformation("Senha  : {Pwd}", SeedPassword);
            _logger.LogInformation("=============================");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Não foi possível criar credencial seed: {Msg}", ex.Message);
        }
    }

    private async Task SeedCommonAreasAsync(string condominiumId)
    {
        var existing = await _commonAreas.GetByCondominiumAsync(condominiumId);
        if (existing.Count > 0) return;

        foreach (var (name, description, capacity, open, close) in MockCommonAreas)
        {
            try
            {
                await _commonAreas.CreateAsync(new CommonArea
                {
                    CondominiumId = condominiumId,
                    Name = name,
                    Description = description,
                    Capacity = capacity,
                    OpenTime = open,
                    CloseTime = close,
                    Rules = "Respeite os horários e mantenha o espaço limpo.",
                    IsActive = true
                });
                _logger.LogInformation("Área comum criada: {Name}", name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Não foi possível criar área comum {Name}: {Msg}", name, ex.Message);
            }
        }
    }

    private async Task SeedMockResidentsAsync(string condominiumId)
    {
        foreach (var (cpf, name) in MockResidents)
        {
            var existing = await _users.GetByCpfAsync(cpf);
            if (existing is not null)
            {
                // Garante que o morador mock está sempre vinculado ao condomínio atual.
                // Resolve o caso em que o banco foi parcialmente limpo e um novo
                // condomínio foi criado com um ObjectId diferente.
                if (existing.CondominiumId != condominiumId)
                {
                    existing.CondominiumId = condominiumId;
                    await _users.UpdateAsync(existing.Id!, existing);
                    _logger.LogInformation("Morador mock atualizado — CPF: {Cpf} | CondominiumId corrigido", cpf);
                }
                continue;
            }

            try
            {
                await _users.CreateAsync(new User
                {
                    CondominiumId = condominiumId,
                    Name = name,
                    Email = $"{cpf}@pending.gestaopro.local",
                    Phone = "00000000000",
                    Cpf = cpf,
                    RegistrationNumber = $"PENDING-{cpf}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = UserRole.morador,
                    MustChangePassword = true,
                    IsActive = true
                });
                _logger.LogInformation("Morador mock criado — CPF: {Cpf} | Nome: {Name}", cpf, name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Não foi possível criar morador mock {Cpf}: {Msg}", cpf, ex.Message);
            }
        }
    }
}
