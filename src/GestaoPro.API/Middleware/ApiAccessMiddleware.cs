using System.Security.Claims;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Middleware;

public class ApiAccessMiddleware
{
    private readonly RequestDelegate _next;

    public ApiAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // O hub SignalR exige login no próprio endpoint (RequireAuthorization em Program.cs)
        // e precisa aceitar qualquer papel: o negotiate é um POST e cairia na regra de escrita abaixo.
        if (HttpMethods.IsOptions(context.Request.Method) ||
            path == "/notificacaohub" || path.StartsWith("/notificacaohub/"))
        {
            await _next(context);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method))
        {
            // Leitura exige login; ficam abertas só a lista pública de condomínios ({id, nome}, tela de cadastro) e o health check.
            var isPublicRead =
                !path.StartsWith("/api/") ||
                path == "/api/condominiums/public" ||
                path.StartsWith("/api/health/");

            if (!isPublicRead && context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Login obrigatório para consultar estes dados." });
                return;
            }

            await _next(context);
            return;
        }

        var hasAdministratorCredential =
            await context.RequestServices.GetRequiredService<IAdministratorLookup>().HasAnyAdministratorAsync();
        var isInitialCredentialCreation =
            HttpMethods.IsPost(context.Request.Method) &&
            path.StartsWith("/api/users/") &&
            path.EndsWith("/credentials") &&
            !hasAdministratorCredential;
        var isFirstUserCreation =
            HttpMethods.IsPost(context.Request.Method) &&
            path == "/api/users" &&
            !hasAdministratorCredential;
        var isFirstCondominiumCreation =
            HttpMethods.IsPost(context.Request.Method) &&
            path == "/api/condominiums" &&
            !hasAdministratorCredential;

        if (path.StartsWith("/api/auth/login") ||
            path.StartsWith("/api/auth/change-password") ||
            path.StartsWith("/api/auth/register") ||
            path.StartsWith("/api/auth/check-cpf") ||
            isInitialCredentialCreation ||
            isFirstUserCreation ||
            isFirstCondominiumCreation)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Login obrigatório para executar esta ação." });
            return;
        }

        var role = context.User.FindFirstValue(ClaimTypes.Role);
        var isAdministrator = role is "admin" or "sindico";
        var isDeliveryPickup =
            path.StartsWith("/api/delivery/") &&
            path.EndsWith("/pickup") &&
            HttpMethods.IsPut(context.Request.Method);
        var isDoormanDeliveryAction =
            role is "porteiro" &&
            path.StartsWith("/api/delivery") &&
            (HttpMethods.IsPost(context.Request.Method) ||
             HttpMethods.IsPut(context.Request.Method) ||
             HttpMethods.IsDelete(context.Request.Method));
        // Ações próprias de quem não administra; o controller confere dono e condomínio.
        var isOwnPasswordChange =
            HttpMethods.IsPatch(context.Request.Method) &&
            path.StartsWith("/api/users/") &&
            path.EndsWith("/change-password");
        var isOwnRecordRemoval =
            HttpMethods.IsDelete(context.Request.Method) &&
            (path.StartsWith("/api/reservations/") || path.StartsWith("/api/occurrences/"));
        var isDoormanVisitorCheck =
            role is "porteiro" &&
            HttpMethods.IsPost(context.Request.Method) &&
            (path.StartsWith("/api/visitor/check-in/") || path.StartsWith("/api/visitor/check-out/"));
        var isUserAllowedAction =
            isOwnPasswordChange ||
            isOwnRecordRemoval ||
            isDoormanVisitorCheck ||
            path.StartsWith("/api/reservations") && HttpMethods.IsPost(context.Request.Method) ||
            path.StartsWith("/api/occurrences") && HttpMethods.IsPost(context.Request.Method) ||
            path.StartsWith("/api/vote") && HttpMethods.IsPost(context.Request.Method) ||
            path == "/api/visitor/register" && HttpMethods.IsPost(context.Request.Method) ||
            isDeliveryPickup ||
            isDoormanDeliveryAction;

        if (!isAdministrator && !isUserAllowedAction)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Ação permitida apenas para administrador ou síndico." });
            return;
        }

        await _next(context);
    }
}
