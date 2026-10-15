using GestaoPro.API.Models;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreateUserRequest(
    [StringLength(100)][Required] string CondominiumId,
    [StringLength(100)] string? UnitId,
    [StringLength(200)] string? UnitLabel,
    [StringLength(200)][Required] string Name,
    [StringLength(254)][Required][EmailAddress] string Email,
    // Opcional: sem senha, a conta nasce com o código do condomínio e troca no primeiro acesso.
    [StringLength(128)] string? Password,
    [StringLength(40)][Required][Phone] string Phone,
    [StringLength(40)][Required][RegularExpression(@"^(\d{11}|\d{3}\.\d{3}\.\d{3}-\d{2})$",
        ErrorMessage = "CPF deve ter 11 números ou seguir o formato 000.000.000-00")] string Cpf,
    [Required] UserRole Role,
    DateTime? BirthDate
);

public record UpdateUserRequest(
    [StringLength(200)] string? Name,
    [StringLength(40)] string? Phone,
    [StringLength(100)] string? UnitId,
    [StringLength(200)] string? UnitLabel,
    [StringLength(254)][EmailAddress] string? Email,
    UserRole? Role,
    DateTime? BirthDate,
    bool? IsActive,
    bool? MustChangePassword
);

public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string CondominiumId { get; set; } = string.Empty;
    public string? UnitId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Retorna a idade do usuário
    public int? Age => BirthDate.HasValue
        ? DateTime.UtcNow.Year - BirthDate.Value.Year -
          (DateTime.UtcNow.DayOfYear < BirthDate.Value.DayOfYear ? 1 : 0)
        : null;
    public string Email { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    // Só na resposta do cadastro sem senha: a senha temporária, mostrada uma única vez.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? TemporaryPassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? BirthDate { get; set; }

    // Retorna a quantidade de tempo de moradia (Somente Sindico e Morador)
    public string? ResidencyDuration =>
    (Role == UserRole.morador || Role == UserRole.sindico) && UnitId != null
        ? $"{DateTime.UtcNow.Year - CreatedAt.Year} ano(s) e " +
          $"{Math.Abs(DateTime.UtcNow.Month - CreatedAt.Month)} mês(es)"
        : null;

    // Retorna a quantidade de tempo desde a inclusão (Para todos User)
    public string MemberSince =>
        $"{DateTime.UtcNow.Year - CreatedAt.Year} ano(s) e " +
        $"{Math.Abs(DateTime.UtcNow.Month - CreatedAt.Month)} mês(es)";
}

// O que quem não administra vê de outros usuários do condomínio: o suficiente para mostrar nomes e unidades,
// sem CPF, e-mail, telefone ou data de nascimento.
public class UserSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string CondominiumId { get; set; } = string.Empty;
    public string? UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }

    public static UserSummaryResponse From(UserResponse user) => new()
    {
        Id = user.Id,
        CondominiumId = user.CondominiumId,
        UnitId = user.UnitId,
        Name = user.Name,
        Role = user.Role,
        IsActive = user.IsActive,
    };
}

public record ChangePasswordRequest(
    [StringLength(128)][Required] string CurrentPassword,
    [StringLength(128)][Required] string NewPassword
);

public record ResetPasswordRequest(
    [StringLength(128)][Required] string NewPassword
);

public record CreateUserCredentialRequest(
    [StringLength(128)][Required] string Password
);

