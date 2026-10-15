using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GestaoPro.API.Controllers;

// Modo demonstração: teto de escritas (POST, PUT, PATCH, DELETE em /api) por conta e por IP, para um script com o
// token de uma conta demo não encher o banco gratuito nem gastar a CPU com BCrypt. Ficam de fora só as rotas
// anônimas, que têm baldes próprios (AuthRateLimit): login, register, check-cpf, change-password e a entrada na demo.
// O balde "por conta" junta usuário e IP: todos os visitantes de um papel usam a mesma conta demo, e um script não
// pode travar os outros. Sem Demo:Enabled, nada muda. Acima do teto: 429 com Retry-After.
public static class DemoWriteLimit
{
    public const string TooManyWritesMessage =
        "Muitas alterações seguidas na demonstração. Aguarde um minuto e tente de novo.";

    public static void AddDemoWriteLimits(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Demo:Enabled", false)) return;

        var perUser = configuration.GetValue("Demo:LimiteEscrita:PorUsuarioPorMinuto", 30);
        var perIp = configuration.GetValue("Demo:LimiteEscrita:PorIpPorMinuto", 60);
        var window = TimeSpan.FromMinutes(1);

        var byIp = PartitionedRateLimiter.Create<HttpContext, string>(context => IsLimitedWrite(context)
            ? RateLimitPartition.GetFixedWindowLimiter("ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "sem-ip"),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perIp, Window = window, QueueLimit = 0 })
            : RateLimitPartition.GetNoLimiter("livre"));
        var byUser = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            IsLimitedWrite(context) && context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId
                ? RateLimitPartition.GetFixedWindowLimiter($"usuario:{userId}|{context.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = perUser, Window = window, QueueLimit = 0 })
                : RateLimitPartition.GetNoLimiter("livre"));

        services.Configure<RateLimiterOptions>(options =>
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(byIp, byUser));
    }

    public static bool IsLimitedWrite(HttpContext context)
    {
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)) return false;
        var path = context.Request.Path;
        return path.StartsWithSegments("/api") && !AnonymousRoutes.Any(route => path.StartsWithSegments(route));
    }

    // Rotas sem login, com limite próprio por IP (e por condomínio no cadastro).
    private static readonly string[] AnonymousRoutes =
    {
        "/api/auth/login", "/api/auth/register", "/api/auth/check-cpf", "/api/auth/change-password", "/api/demo",
    };
}
