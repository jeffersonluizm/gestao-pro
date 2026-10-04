using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

// Tradução das decisões de ResourceAccess para HTTP, igual em todos os controllers:
// 403 com { message } quando logado sem permissão; 404 igual ao de inexistente para outro condomínio.
public static class AccessResults
{
    public const string ForbiddenMessage = "Você não tem permissão para consultar estes dados.";
    public const string InvalidSessionMessage = "Sessão sem condomínio. Saia e entre novamente.";

    public static Caller Caller(this ControllerBase controller) =>
        Services.Access.Caller.From(controller.User);

    // null quando permitido; senão, a resposta a devolver.
    public static ActionResult? Deny(this ControllerBase controller, AccessDecision decision) => decision switch
    {
        AccessDecision.Allowed => null,
        AccessDecision.NotFound => controller.NotFound(),
        AccessDecision.InvalidSession => controller.StatusCode(StatusCodes.Status403Forbidden, new { message = InvalidSessionMessage }),
        _ => controller.Forbidden(),
    };

    // Escrita da equipe só no próprio condomínio: null quando permitido.
    public static ActionResult? DenyManage(this ControllerBase controller, Caller caller, string? condominiumId, bool staff) =>
        controller.Deny(ResourceAccess.DecideManage(caller, condominiumId, staff));

    // Leitura de dado do condomínio (área, aviso, assembleia, unidade, vaga): só o do token; outro ou sem condomínio: 404.
    public static ActionResult? DenyOtherCondominium(this ControllerBase controller, Caller caller, string? condominiumId)
    {
        if (!caller.HasSession) return controller.Deny(AccessDecision.InvalidSession);
        return string.Equals(condominiumId, caller.CondominiumId, StringComparison.Ordinal) ? null : controller.NotFound();
    }

    // Filtro das listas de dados do condomínio: só os do condomínio do token.
    public static List<T> InCondominium<T>(this Caller caller, IEnumerable<T> items, Func<T, string?> condominiumOf) =>
        items.Where(item => string.Equals(condominiumOf(item), caller.CondominiumId, StringComparison.Ordinal)).ToList();

    public static ActionResult Forbidden(this ControllerBase controller) =>
        controller.StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenMessage });
}
