using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class AuthAppService
{
    private readonly CredentialRepository _repository;
    private readonly UserRepository _userRepository;
    private readonly TokenService _tokenService;

    public AuthAppService(
        CredentialRepository repository,
        UserRepository userRepository,
        TokenService tokenService)
    {
        _repository = repository;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        request = request with
        {
            RegistrationNumber = CleanLegacyValue(request.RegistrationNumber),
            Password = request.Password.Trim()
        };

        // Busca a credencial pela matrícula
        var credential = await _repository.GetByRegistrationNumberAsync(request.RegistrationNumber);

        if (credential is null)
        {
            credential = await TryCreateCredentialFromExistingUserAsync(request);
        }

        if (credential is null)
            credential = await TryFindCredentialByEmailAsync(request);

        if (credential is null || !credential.IsActive)
            throw new InvalidOperationException("E-mail ou senha inválidos.");

        if (!PasswordMatches(request.Password, credential.PasswordHash))
            throw new InvalidOperationException("E-mail ou senha inválidos.");

        // Gera o token JWT
        var (token, expiresAt) = _tokenService.GenerateToken(credential);
        var user = await _userRepository.GetByIdAsync(credential.UserId);
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

    private async Task<Credential?> TryFindCredentialByEmailAsync(LoginRequest request)
    {
        var email = request.RegistrationNumber.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Id))
            return null;

        if (!PasswordMatches(request.Password, user.PasswordHash))
            return null;

        return await _repository.GetByUserIdAsync(user.Id);
    }

    private async Task<Credential?> TryCreateCredentialFromExistingUserAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByRegistrationNumberIncludingLegacyAsync(request.RegistrationNumber);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Id))
            return null;

        if (!PasswordMatches(request.Password, user.PasswordHash))
            return null;

        var passwordHash = IsBcryptHash(user.PasswordHash)
            ? user.PasswordHash
            : BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (passwordHash != user.PasswordHash)
        {
            await _userRepository.UpdatePasswordAsync(user.Id, passwordHash);
        }

        var credential = new Credential
        {
            UserId = user.Id,
            RegistrationNumber = user.RegistrationNumber,
            PasswordHash = passwordHash,
            Role = user.Role.ToString(),
            CondominiumId = user.CondominiumId,
            MustChangePassword = user.MustChangePassword,
            IsActive = user.IsActive
        };

        await _repository.CreateAsync(credential);
        return credential;
    }

    private static bool PasswordMatches(string password, string storedPassword)
    {
        storedPassword = CleanLegacyValue(storedPassword);

        if (string.IsNullOrWhiteSpace(storedPassword))
            return false;

        if (IsBcryptHash(storedPassword))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedPassword);
            }
            catch
            {
                return false;
            }
        }

        return storedPassword == CleanLegacyValue(password);
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
        // Verifica se já existe credencial para esse usuário
        var existing = await _repository.GetByUserIdAsync(userId);
        if (existing is not null)
            throw new InvalidOperationException("Já existe uma credencial para esse usuário.");

        var credential = new Credential
        {
            UserId = userId,
            RegistrationNumber = registrationNumber,
            PasswordHash = passwordHash,
            Role = role,
            CondominiumId = condominiumId,
            MustChangePassword = true
        };

        await _repository.CreateAsync(credential);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(AuthResetPasswordRequest request)
    {
        var credential = await _repository.GetByUserIdAsync(request.UserId);
        if (credential is null)
            throw new InvalidOperationException("Credencial não encontrada.");

        await _repository.UpdatePasswordAsync(
            request.UserId,
            BCrypt.Net.BCrypt.HashPassword(request.NewPassword), // faz o hash aqui
            true);
        return true;
    }

    public async Task<bool> ResetPasswordHashAsync(string userId, string passwordHash)
    {
        var credential = await _repository.GetByUserIdAsync(userId);
        if (credential is null)
            throw new InvalidOperationException("Credencial não encontrada.");

        await _repository.UpdatePasswordAsync(userId, passwordHash, true);
        return true;
    }

    // Verifica a senha atual contra a credential (fallback para ChangePasswordAsync do UserAppService).
    public async Task<bool> VerifyCredentialPasswordAsync(string userId, string password)
    {
        var credential = await _repository.GetByUserIdAsync(userId);
        if (credential is null) return false;
        return PasswordMatches(password, credential.PasswordHash);
    }

    // Sincroniza a senha da credential sem alterar o flag mustChangePassword.
    // Usado por UserAppService após atualizar user.PasswordHash.
    public async Task SyncCredentialPasswordAsync(string userId, string newPasswordHash)
    {
        var credential = await _repository.GetByUserIdAsync(userId);
        if (credential is null) return; // sem credential ainda — será criada no próximo login
        await _repository.UpdatePasswordAsync(userId, newPasswordHash, credential.MustChangePassword);
    }

    public async Task<bool> DeactivateAsync(string userId)
    {
        var credential = await _repository.GetByUserIdAsync(userId);
        if (credential is null) return false;
        await _repository.DeactivateAsync(userId);
        return true;
    }
}

