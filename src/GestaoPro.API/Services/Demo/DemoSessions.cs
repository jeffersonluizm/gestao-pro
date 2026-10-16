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
        // Só a conta demo semeada: condomínio demo, papel do catálogo e usuário com o e-mail da conta.
        if (credential.CondominiumId != _catalog.CondominiumId || credential.Role != account.Role) return null;
        var user = await _accounts.GetUserByIdAsync(credential.UserId);
        if (user is null || user.Id != credential.UserId || user.CondominiumId != _catalog.CondominiumId ||
            !string.Equals(user.Email, account.Email, StringComparison.OrdinalIgnoreCase)) return null;

        var (token, expiresAt) = _tokens.GenerateToken(credential);
        return new LoginResponse(token, credential.UserId, user?.Name ?? account.Name, credential.RegistrationNumber,
            credential.Role, credential.CondominiumId, false, expiresAt);
    }
}
