using System.Security.Claims;

namespace GestaoPro.API.Services.Access;

public enum AccessDecision
{
    Allowed,
    // Logado e no mesmo condomínio, mas o dado é de outra pessoa: 403.
    Forbidden,
    // Dado de outro condomínio: 404 igual ao de inexistente, para não revelar que existe.
    NotFound,
    // Token sem usuário ou sem condomínio (emitido antes da claim ou de credencial incompleta): 403, entrar de novo.
    InvalidSession,
}

// Quem está chamando, lido das claims emitidas pelo TokenService.
public sealed record Caller(string? UserId, string? Role, string? CondominiumId)
{
    public const string CondominiumIdClaim = "condominiumId";

    public static Caller From(ClaimsPrincipal user) => new(
        user.FindFirstValue(ClaimTypes.NameIdentifier),
        user.FindFirstValue(ClaimTypes.Role),
        user.FindFirstValue(CondominiumIdClaim));

    public bool IsAdministrator => Role is "admin" or "sindico";

    public bool IsDoorman => Role is "porteiro";

    public bool HasSession => !string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(CondominiumId);
}

// Regra única de leitura de dados pessoais (cobranças, pagamentos, reservas, visitantes, usuários).
public static class ResourceAccess
{
    // condominiumWide: o chamador pode ver os itens de todo o próprio condomínio (administração; porteiro em visitantes).
    public static AccessDecision Decide(Caller caller, string? ownerId, string? condominiumId, bool condominiumWide)
    {
        if (!caller.HasSession) return AccessDecision.InvalidSession;

        var hasCondominium = !string.IsNullOrWhiteSpace(condominiumId);
        if (hasCondominium && !string.Equals(condominiumId, caller.CondominiumId, StringComparison.Ordinal))
            return AccessDecision.NotFound;

        if (string.Equals(ownerId, caller.UserId, StringComparison.Ordinal)) return AccessDecision.Allowed;

        // Registro legado sem condomínio gravado: sem como provar o condomínio, só o dono vê.
        return condominiumWide && hasCondominium ? AccessDecision.Allowed : AccessDecision.Forbidden;
    }

    public static bool CanSee(Caller caller, string? ownerId, string? condominiumId, bool condominiumWide) =>
        Decide(caller, ownerId, condominiumId, condominiumWide) == AccessDecision.Allowed;
}
