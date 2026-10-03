using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record LoginRequest(
    [Required] string RegistrationNumber,
    [Required] string Password
);

public record LoginResponse(
    string Token,
    string UserId,
    string UserName,
    string RegistrationNumber,
    string Role,
    string CondominiumId,
    bool MustChangePassword,
    DateTime ExpiresAt
);

public record CreateCredentialRequest(
    [Required] string UserId,
    [Required] string RegistrationNumber,
    [Required] string PasswordHash,
    [Required] string Role,
    [Required] string CondominiumId
);

public record AuthChangePasswordRequest(
    [Required] string UserId,
    [Required] string CurrentPassword,
    [Required] string NewPassword
);

public record AuthResetPasswordRequest(
    [Required] string UserId,
    [Required] string NewPassword // texto puro, não hash
);

public record SelfRegisterRequest(
    [Required] string Cpf,
    [Required] string CondominiumId,
    [Required] string Password,
    string? Name,
    string? Email,
    string? Phone
);

public record SelfRegisterResponse(string RegistrationNumber, string Name);

public record CheckCpfRequest(
    [Required] string Cpf,
    [Required] string CondominiumId
);

public record CheckCpfResponse(
    bool Found,
    bool HasCredential,
    bool NeedsProfileData,
    string? Name
);

