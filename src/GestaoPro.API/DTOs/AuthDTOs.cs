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

// A senha chega em texto (HTTPS) e a API aplica a política e gera o hash: nunca aceita hash vindo do cliente.
public record CreateCredentialRequest(
    [Required] string UserId,
    [Required] string RegistrationNumber,
    [Required] string Password,
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
    // Senha padrão do condomínio, entregue pela administração: prova o vínculo com o condomínio.
    [Required] string CondominiumCode,
    [Required] string Password,
    string? Name,
    string? Email,
    string? Phone
);

public record SelfRegisterResponse(string RegistrationNumber, string Name);

public record CheckCpfRequest(
    [Required] string Cpf,
    [Required] string CondominiumId,
    [Required] string CondominiumCode
);

// Só diz se o cadastro pode seguir: sem nome nem motivo da recusa (a mensagem é sempre a mesma).
public record CheckCpfResponse(
    bool CanProceed,
    bool NeedsProfileData,
    string Message
);

