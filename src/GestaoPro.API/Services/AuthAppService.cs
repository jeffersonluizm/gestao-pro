using System.Security.Cryptography;
using System.Text;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

// Dados de autenticação (credenciais e usuários), separados do MongoDB para os testes.
public interface IAuthAccounts
{
    Task<Credential?> GetCredentialByRegistrationNumberAsync(string registrationNumber);
    Task<Credential?> GetCredentialByUserIdAsync(string userId);
    Task CreateCredentialAsync(Credential credential);
    Task UpdateCredentialPasswordAsync(string userId, string passwordHash, bool mustChangePassword);
    Task DeactivateCredentialAsync(string userId);
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByRegistrationNumberIncludingLegacyAsync(string registrationNumber);
    Task UpdateUserPasswordAsync(string userId, string passwordHash);
}

public class MongoAuthAccounts : IAuthAccounts
{
    private readonly CredentialRepository _credentials;
    private readonly UserRepository _users;

    public MongoAuthAccounts(CredentialRepository credentials, UserRepository users)
    {
        _credentials = credentials;
        _users = users;
    }

    public Task<Credential?> GetCredentialByRegistrationNumberAsync(string registrationNumber) =>
        _credentials.GetByRegistrationNumberAsync(registrationNumber);

    public Task<Credential?> GetCredentialByUserIdAsync(string userId) => _credentials.GetByUserIdAsync(userId);

    public Task CreateCredentialAsync(Credential credential) => _credentials.CreateAsync(credential);

    public Task UpdateCredentialPasswordAsync(string userId, string passwordHash, bool mustChangePassword) =>
        _credentials.UpdatePasswordAsync(userId, passwordHash, mustChangePassword);

    public Task DeactivateCredentialAsync(string userId) => _credentials.DeactivateAsync(userId);

    public Task<User?> GetUserByIdAsync(string id) => _users.GetByIdAsync(id);

    public Task<User?> GetUserByEmailAsync(string email) => _users.GetByEmailAsync(email);

    public Task<User?> GetUserByRegistrationNumberIncludingLegacyAsync(string registrationNumber) =>
        _users.GetByRegistrationNumberIncludingLegacyAsync(registrationNumber);

    public Task UpdateUserPasswordAsync(string userId, string passwordHash) => _users.UpdatePasswordAsync(userId, passwordHash);
}

// Login e credenciais.
// Toda tentativa de login roda exatamente uma verificação BCrypt (com um hash fixo quando não há conta
// utilizável), para o tempo de resposta não revelar se o e-mail está cadastrado ou se a conta está ativa.
public class AuthAppService
{
    public const string InvalidLoginMessage = "E-mail ou senha inválidos.";

    // Hash de um valor aleatório com o custo padrão: gasta o mesmo tempo quando não há o que conferir.
    private static readonly string NoAccountHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

    private readonly IAuthAccounts _accounts;
    private readonly IPasswordVerifier _verifier;
    private readonly TokenService _tokenService;

    public AuthAppService(IAuthAccounts accounts, IPasswordVerifier verifier, TokenService tokenService)
    {
        _accounts = accounts;
        _verifier = verifier;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var login = CleanLegacyValue(request.RegistrationNumber);
        var password = request.Password.Trim();

        // 1) credencial pela matrícula/e-mail de login; 2) pelo e-mail do usuário (contas criadas pela administração,
        // cuja credencial guarda a matrícula); 3) usuário legado sem credencial (criada no primeiro login).
        var credential = await _accounts.GetCredentialByRegistrationNumberAsync(login);
        if (credential is null)
        {
            var byEmail = await _accounts.GetUserByEmailAsync(login.ToLowerInvariant());
            if (byEmail is { IsActive: true } && !string.IsNullOrWhiteSpace(byEmail.Id))
                credential = await _accounts.GetCredentialByUserIdAsync(byEmail.Id);
        }

        User? legacyUser = null;
        if (credential is null)
        {
            legacyUser = await _accounts.GetUserByRegistrationNumberIncludingLegacyAsync(login);
            if (legacyUser is not { IsActive: true } || string.IsNullOrWhiteSpace(legacyUser.Id)) legacyUser = null;
        }

        var storedHash = credential is { IsActive: true } ? credential.PasswordHash : legacyUser?.PasswordHash;
        if (!PasswordMatches(password, storedHash))
            throw new InvalidOperationException(InvalidLoginMessage);

        credential ??= await CreateCredentialForLegacyUserAsync(legacyUser!, password);

        var (token, expiresAt) = _tokenService.GenerateToken(credential);
        var user = await _accounts.GetUserByIdAsync(credential.UserId);
        var userName = user?.Name ?? credential.RegistrationNumber;

        return new LoginResponse(
            token,
            credential.UserId,
            userName,
            credential.RegistrationNumber,
            credential.Role,
            credential.CondominiumId,
            credential.MustChangePassword,
            expiresAt
        );
    }

    // Usuário antigo (sem credencial) que acertou a senha: ganha credencial, com a senha migrada para BCrypt.
    private async Task<Credential> CreateCredentialForLegacyUserAsync(User user, string password)
    {
        var storedHash = CleanLegacyValue(user.PasswordHash);
        var passwordHash = IsBcryptHash(storedHash) ? storedHash : BCrypt.Net.BCrypt.HashPassword(password);

        if (passwordHash != user.PasswordHash)
            await _accounts.UpdateUserPasswordAsync(user.Id!, passwordHash);

        var credential = new Credential
        {
            UserId = user.Id!,
            RegistrationNumber = user.RegistrationNumber,
            PasswordHash = passwordHash,
            Role = user.Role.ToString(),
            CondominiumId = user.CondominiumId,
            MustChangePassword = user.MustChangePassword,
            IsActive = user.IsActive
        };

        await _accounts.CreateCredentialAsync(credential);
        return credential;
    }

    // Exatamente uma verificação BCrypt por chamada, qualquer que seja o valor guardado.
    private bool PasswordMatches(string password, string? storedPassword)
    {
        var stored = CleanLegacyValue(storedPassword ?? string.Empty);

        if (IsBcryptHash(stored))
            return _verifier.Verify(password, stored);

        // Sem conta utilizável ou senha legada em texto: gasta o tempo de um BCrypt e compara em tempo constante.
        _verifier.Verify(password, NoAccountHash);
        return stored.Length > 0 && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(stored),
            Encoding.UTF8.GetBytes(CleanLegacyValue(password)));
    }

    private static bool IsBcryptHash(string value) =>
        value.StartsWith("$2a$") ||
        value.StartsWith("$2b$") ||
        value.StartsWith("$2y$");

    private static string CleanLegacyValue(string value) =>
        value.Trim().Trim(',', '/', '\\').Trim();

    // Rota da administração: a senha vem em texto e o hash é gerado aqui (a política já foi conferida no controller).
    public Task<bool> CreateCredentialAsync(CreateCredentialRequest request) =>
        CreateCredentialWithHashAsync(
            request.UserId,
            request.RegistrationNumber,
            BCrypt.Net.BCrypt.HashPassword(request.Password),
            request.Role,
            request.CondominiumId);

    // Uso interno (UserAppService), com o hash já calculado no servidor.
    public async Task<bool> CreateCredentialWithHashAsync(
        string userId, string registrationNumber, string passwordHash, string role, string condominiumId)
    {
        var existing = await _accounts.GetCredentialByUserIdAsync(userId);
        if (existing is not null)
            throw new InvalidOperationException("Já existe uma credencial para esse usuário.");

        await _accounts.CreateCredentialAsync(new Credential
        {
            UserId = userId,
            RegistrationNumber = registrationNumber,
            PasswordHash = passwordHash,
            Role = role,
            CondominiumId = condominiumId,
            MustChangePassword = true
        });
        return true;
    }

    public async Task<bool> ResetPasswordAsync(AuthResetPasswordRequest request)
    {
        var credential = await _accounts.GetCredentialByUserIdAsync(request.UserId);
        if (credential is null)
            throw new InvalidOperationException("Credencial não encontrada.");

        await _accounts.UpdateCredentialPasswordAsync(
            request.UserId,
            BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
            true);
        return true;
    }

    public async Task<bool> ResetPasswordHashAsync(string userId, string passwordHash)
    {
        var credential = await _accounts.GetCredentialByUserIdAsync(userId);
        if (credential is null)
            throw new InvalidOperationException("Credencial não encontrada.");

        await _accounts.UpdateCredentialPasswordAsync(userId, passwordHash, true);
        return true;
    }

    // Verifica a senha atual contra a credencial (fallback para ChangePasswordAsync do UserAppService).
    public async Task<bool> VerifyCredentialPasswordAsync(string userId, string password)
    {
        var credential = await _accounts.GetCredentialByUserIdAsync(userId);
        if (credential is null) return false;
        return PasswordMatches(password, credential.PasswordHash);
    }

    // Sincroniza a senha da credencial sem alterar o flag mustChangePassword.
    // Usado por UserAppService após atualizar user.PasswordHash.
    public async Task SyncCredentialPasswordAsync(string userId, string newPasswordHash)
    {
        var credential = await _accounts.GetCredentialByUserIdAsync(userId);
        if (credential is null) return; // sem credencial ainda: será criada no próximo login
        await _accounts.UpdateCredentialPasswordAsync(userId, newPasswordHash, credential.MustChangePassword);
    }

    public async Task<bool> DeactivateAsync(string userId)
    {
        var credential = await _accounts.GetCredentialByUserIdAsync(userId);
        if (credential is null) return false;
        await _accounts.DeactivateCredentialAsync(userId);
        return true;
    }
}
