using GestaoPro.API.DTOs;

namespace GestaoPro.API.Services.Demo;

public interface IDemoSessions
{
    // Sessão pronta da conta demo do papel, ou null se a conta ainda não existe (demo sendo preparada).
    Task<LoginResponse?> EnterAsync(string role);
}

// POST /api/demo/entrar: a sessão da conta demo sai sem senha nenhuma (a conta nem tem senha utilizável).
public sealed class DemoSessions : IDemoSessions
{
    private readonly DemoCatalog _catalog;
    private readonly IAuthAccounts _accounts;
    private readonly TokenService _tokens;

    public DemoSessions(DemoCatalog catalog, IAuthAccounts accounts, TokenService tokens)
    {
        _catalog = catalog;
        _accounts = accounts;
        _tokens = tokens;
    }

    public async Task<LoginResponse?> EnterAsync(string role)
    {
        var account = _catalog.AccountFor(role);
        if (!_catalog.Enabled || account is null) return null;

        var credential = await _accounts.GetCredentialByRegistrationNumberAsync(account.Email);
        if (credential is not { IsActive: true }) return null;

        var (token, expiresAt) = _tokens.GenerateToken(credential);
        var user = await _accounts.GetUserByIdAsync(credential.UserId);
        return new LoginResponse(token, credential.UserId, user?.Name ?? account.Name, credential.RegistrationNumber,
            credential.Role, credential.CondominiumId, false, expiresAt);
    }
}
