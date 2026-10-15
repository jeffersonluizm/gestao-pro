using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record LoginRequest(
    [StringLength(100)][Required] string RegistrationNumber,
    [StringLength(128)][Required] string Password
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
    [StringLength(100)][Required] string UserId,
    [StringLength(100)][Required] string RegistrationNumber,
    [StringLength(128)][Required] string Password,
    [StringLength(50)][Required] string Role,
    [StringLength(100)][Required] string CondominiumId
);

public record AuthChangePasswordRequest(
    [StringLength(100)][Required] string UserId,
    [StringLength(128)][Required] string CurrentPassword,
    [StringLength(128)][Required] string NewPassword
);

public record AuthResetPasswordRequest(
    [StringLength(100)][Required] string UserId,
    [StringLength(128)][Required] string NewPassword // texto puro, não hash
);

public record SelfRegisterRequest(
    [StringLength(40)][Required] string Cpf,
    [StringLength(100)][Required] string CondominiumId,
    // Senha padrão do condomínio, entregue pela administração: prova o vínculo com o condomínio.
    [StringLength(100)][Required] string CondominiumCode,
    [StringLength(128)][Required] string Password,
    [StringLength(200)] string? Name,
    [StringLength(254)] string? Email,
    [StringLength(40)] string? Phone
);

public record SelfRegisterResponse(string RegistrationNumber, string Name);

public record CheckCpfRequest(
    [StringLength(40)][Required] string Cpf,
    [StringLength(100)][Required] string CondominiumId,
    [StringLength(100)][Required] string CondominiumCode
);

// Só diz se o cadastro pode seguir: sem nome nem motivo da recusa (a mensagem é sempre a mesma).
public record CheckCpfResponse(
    bool CanProceed,
    bool NeedsProfileData,
    string Message
);

