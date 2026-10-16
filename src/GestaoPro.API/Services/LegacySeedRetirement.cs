using GestaoPro.API.Repositories;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface ILegacySeedStore
{
    Task<Credential?> GetCredentialAsync(string registrationNumber);
    Task DeactivateCredentialAsync(string userId);
    Task<string?> GetCondominiumCodeHashAsync(string cnpj);
    Task ReplaceCondominiumCodeHashAsync(string cnpj, string newHash);
}

public sealed class MongoLegacySeedStore : ILegacySeedStore
{
    private readonly CredentialRepository _credentials;
    private readonly CondominiumRepository _condominiums;

    public MongoLegacySeedStore(CredentialRepository credentials, CondominiumRepository condominiums)
    {
        _credentials = credentials;
        _condominiums = condominiums;
    }

    public Task<Credential?> GetCredentialAsync(string registrationNumber) => _credentials.GetByRegistrationNumberAsync(registrationNumber);

    public Task DeactivateCredentialAsync(string userId) => _credentials.DeactivateAsync(userId);

    public async Task<string?> GetCondominiumCodeHashAsync(string cnpj) => (await _condominiums.GetByCnpjAsync(cnpj))?.DefaultPassword;

    public async Task ReplaceCondominiumCodeHashAsync(string cnpj, string newHash)
    {
        var condominium = await _condominiums.GetByCnpjAsync(cnpj);
        if (condominium is null) return;
        condominium.DefaultPassword = newHash;
        await _condominiums.UpdateAsync(condominium.Id!, condominium);
    }
}

// Aposenta os dados de demonstração das versões antigas, que usavam valores públicos (estavam no código e na tela
// de login). Os valores abaixo existem só para RECONHECER e desativar esses dados; nenhuma conta é criada com eles.
public sealed class LegacySeedRetirement
{
    public const string LegacyAdminEmail = "admin@gestaopro.local";
    public const string LegacyAdminPassword = "Admin@123";
    public const string LegacyCondominiumCnpj = "00.000.000/0001-00";
    // A primeira versão usava a senha do admin como código; uma correção posterior passou a usar Demo-Condominio-2026.
    public static readonly string[] LegacyCondominiumCodes = [LegacyAdminPassword, "Demo-Condominio-2026"];

    private readonly ILegacySeedStore _store;
    private readonly ILogger<LegacySeedRetirement> _logger;

    public LegacySeedRetirement(ILegacySeedStore store, ILogger<LegacySeedRetirement> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var credential = await _store.GetCredentialAsync(LegacyAdminEmail);
        if (credential is { IsActive: true } && Matches(LegacyAdminPassword, credential.PasswordHash))
        {
            await _store.DeactivateCredentialAsync(credential.UserId);
            _logger.LogWarning("Conta administradora antiga com senha pública desativada ({Email}).", LegacyAdminEmail);
        }

        var codeHash = await _store.GetCondominiumCodeHashAsync(LegacyCondominiumCnpj);
        if (codeHash is not null && LegacyCondominiumCodes.Any(code => Matches(code, codeHash)))
        {
            await _store.ReplaceCondominiumCodeHashAsync(LegacyCondominiumCnpj, BCrypt.Net.BCrypt.HashPassword(TemporaryPassword.Generate()));
            _logger.LogWarning("Código público do condomínio de demonstração antigo trocado por um aleatório.");
        }
    }

    private static bool Matches(string text, string? hash)
    {
        try { return !string.IsNullOrWhiteSpace(hash) && BCrypt.Net.BCrypt.Verify(text, hash); }
        catch { return false; }
    }
}

// Roda na partida, em segundo plano: falha (banco fora) só gera log.
public sealed class LegacySeedRetirementInitializer : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LegacySeedRetirementInitializer> _logger;

    public LegacySeedRetirementInitializer(IServiceScopeFactory scopes, ILogger<LegacySeedRetirementInitializer> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LegacySeedRetirement>().RunAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível conferir os dados de demonstração antigos.");
        }
    }
}
