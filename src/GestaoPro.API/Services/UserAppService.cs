
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Services;

public class UserAppService : IUserDirectory
{
    private readonly UserRepository _userRepository;
    private readonly UnitRepository _unitRepository;
    private readonly CondominiumAppService _condominiumAppService;
    private readonly AuthAppService _authAppService;

    public UserAppService(
        UserRepository userRepository,
        UnitRepository unitRepository,
        CondominiumAppService condominiumAppService,
        AuthAppService authAppService)
    {
        _userRepository = userRepository;
        _unitRepository = unitRepository;
        _condominiumAppService = condominiumAppService;
        _authAppService = authAppService;
    }

    public async Task<List<UserResponse>> GetAllActiveAsync()
    {
        var users = await _userRepository.GetAllActiveAsync();
        return users.Select(MapToResponse).ToList();
    }

    public async Task<List<UserResponse>> GetAllInactiveAsync()
    {
        var users = await _userRepository.GetAllInactiveAsync();
        return users.Select(MapToResponse).ToList();
    }

    public async Task<List<UserResponse>> GetByCondominiumAsync(string condominiumId)
    {
        var users = await _userRepository.GetByCondominiumAsync(condominiumId);
        return users.Select(MapToResponse).ToList();
    }

    public async Task<UserResponse?> GetByIdAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var cpf = OnlyDigits(request.Cpf);
        var unitId = await ResolveUnitIdAsync(request.CondominiumId, request.UnitId, request.UnitLabel);
        if (unitId is null && !string.IsNullOrWhiteSpace(request.UnitId))
        {
            throw new InvalidOperationException("Unidade não encontrada. Cadastre a unidade antes ou confira o valor informado, por exemplo: 201.");
        }

        var existingEmail = await _userRepository.GetByEmailAsync(email);
        if (existingEmail is not null)
            throw new InvalidOperationException("Email já cadastrado no sistema.");

        var existingCpf = await _userRepository.GetByCpfAsync(cpf);
        if (existingCpf is not null)
            throw new InvalidOperationException("CPF já cadastrado no sistema.");

        var condominiumDefaultPasswordHash = await _condominiumAppService.GetDefaultPasswordHashAsync(request.CondominiumId);
        if (condominiumDefaultPasswordHash is null)
            throw new InvalidOperationException("Condomínio não encontrado.");

        Unit? unit = null;
        if (unitId is not null)
        {
            unit = await _unitRepository.GetByIdAsync(unitId);
            if (unit is null)
                throw new InvalidOperationException("Unidade não encontrada.");

            foreach (var residentId in unit.ResidentIds)
            {
                var resident = await _userRepository.GetByIdAsync(residentId);
                if (resident is not null && OnlyDigits(resident.Cpf) == cpf)
                    throw new InvalidOperationException("Já existe um morador com esse CPF nessa unidade.");
            }
        }

        var registrationNumber = $"COND-{DateTime.UtcNow.Year}-{Random.Shared.Next(1000, 9999)}";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            CondominiumId = request.CondominiumId,
            UnitId = unitId,
            Name = request.Name.Trim(),
            Email = email,
            RegistrationNumber = registrationNumber,
            PasswordHash = passwordHash,
            MustChangePassword = true,
            Phone = OnlyDigits(request.Phone),
            Cpf = cpf,
            Role = request.Role,
            BirthDate = request.BirthDate
        };

        try
        {
            var created = await _userRepository.CreateAsync(user);

            if (unitId is not null)
            {
                await _unitRepository.AddResidentAsync(unitId, created.Id!);
            }

            await _authAppService.CreateCredentialAsync(new CreateCredentialRequest(
                created.Id!,
                registrationNumber,
                passwordHash,
                request.Role.ToString(),
                request.CondominiumId
            ));

            return MapToResponse(created);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException("Já existe um usuário cadastrado com esse e-mail, CPF ou matrícula.");
        }
    }

    public async Task<UserResponse?> UpdateAsync(string id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return null;

        if (request.Name is not null) user.Name = request.Name;
        if (request.Phone is not null) user.Phone = request.Phone;
        if (request.Email is not null) user.Email = request.Email;
        if (request.Role is not null) user.Role = request.Role.Value;
        if (request.BirthDate is not null) user.BirthDate = request.BirthDate;
        if (request.IsActive is not null) user.IsActive = request.IsActive.Value;
        if (request.MustChangePassword is not null) user.MustChangePassword = request.MustChangePassword.Value;

        var nextUnitId = await ResolveUnitIdAsync(user.CondominiumId, request.UnitId, request.UnitLabel);
        if (nextUnitId is null && !string.IsNullOrWhiteSpace(request.UnitId))
        {
            throw new InvalidOperationException("Unidade não encontrada. Cadastre a unidade antes ou confira o valor informado, por exemplo: 201.");
        }

        if (nextUnitId is not null && nextUnitId != user.UnitId)
        {
            if (user.UnitId is not null)
                await _unitRepository.RemoveResidentAsync(user.UnitId, id);

            var unit = await _unitRepository.GetByIdAsync(nextUnitId);
            if (unit is not null)
            {
                foreach (var existingResidentId in unit.ResidentIds)
                {
                    var existingResident = await _userRepository.GetByIdAsync(existingResidentId);
                    if (existingResident is not null && existingResident.Cpf == user.Cpf)
                        throw new InvalidOperationException("Já existe um morador com esse CPF nessa unidade.");
                }
                await _unitRepository.AddResidentAsync(nextUnitId, id);
            }
            user.UnitId = nextUnitId;
        }

        await _userRepository.UpdateAsync(id, user);
        return MapToResponse(user);
    }

    public async Task<bool> DeactivateAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return false;
        await _userRepository.DeactivateAsync(id);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string id, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return false;

        // Verifica contra user.PasswordHash (fonte primária para usuários sem credential ainda)
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            // Fallback: verifica contra a credential (usuários já migrados para login por e-mail)
            var credentialOk = await _authAppService.VerifyCredentialPasswordAsync(id, request.CurrentPassword);
            if (!credentialOk)
                throw new InvalidOperationException("Senha atual incorreta.");
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Atualiza as duas coleções atomicamente para manter consistência
        await _userRepository.UpdatePasswordAsync(id, newHash);
        await _authAppService.SyncCredentialPasswordAsync(id, newHash);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return false;

        var defaultPasswordHash = await _condominiumAppService.GetDefaultPasswordHashAsync(user.CondominiumId);
        if (defaultPasswordHash is null)
            throw new InvalidOperationException("Condomínio não encontrado.");

        await _userRepository.UpdatePasswordAsync(id, defaultPasswordHash);
        await _authAppService.ResetPasswordHashAsync(id, defaultPasswordHash);

        return true;
    }

    public async Task<bool> UpdatePasswordByAuthAsync(string id, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return false;

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userRepository.UpdatePasswordAsync(id, newHash);
        return true;
    }

    public async Task<UserResponse?> CreateCredentialForExistingUserAsync(string id, CreateUserCredentialRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) return null;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await _userRepository.UpdatePasswordAsync(id, passwordHash);

        await _authAppService.CreateCredentialAsync(new CreateCredentialRequest(
            user.Id!,
            user.RegistrationNumber,
            passwordHash,
            user.Role.ToString(),
            user.CondominiumId
        ));

        return MapToResponse(user);
    }

    private static UserResponse MapToResponse(User u) => new()
    {
        Id = u.Id!,
        CondominiumId = u.CondominiumId,
        UnitId = u.UnitId,
        Name = u.Name,
        Email = u.Email,
        RegistrationNumber = u.RegistrationNumber,
        Phone = u.Phone,
        Cpf = u.Cpf,
        Role = u.Role,
        IsActive = u.IsActive,
        MustChangePassword = u.MustChangePassword,
        CreatedAt = u.CreatedAt,
        BirthDate = u.BirthDate
    };

    private static string OnlyDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    // Só aceita unidade do próprio condomínio (ver UnitIdResolver).
    private Task<string?> ResolveUnitIdAsync(string condominiumId, string? unitIdOrLabel, string? unitLabel) =>
        UnitIdResolver.ResolveAsync(
            condominiumId, unitIdOrLabel, unitLabel,
            _unitRepository.GetByIdAsync,
            _unitRepository.FindByLabelAsync);
}

