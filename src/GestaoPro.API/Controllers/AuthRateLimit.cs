using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GestaoPro.API.Controllers;

// Limites das rotas anônimas de autenticação. Cada grupo tem o próprio balde por IP (estourar o login não
// bloqueia o cadastro); check-cpf e register também têm um balde por condomínio (CondominiumAttemptLimiter),
// contra força bruta do código do condomínio vinda de vários IPs. Acima do limite: 429 com Retry-After.
public static class AuthRateLimit
{
    public const string Login = "auth-login";
    public const string Register = "auth-register";
    public const string ChangePassword = "auth-password";

    public const string TooManyAttemptsMessage = "Muitas tentativas. Aguarde e tente de novo.";

    public static void AddAuthRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));
        var perCondominiumLimit = configuration.GetValue("RateLimiting:Auth:PerCondominiumPermitLimit", 30);

        services.AddSingleton(new CondominiumAttemptLimiter(perCondominiumLimit, window));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            foreach (var policy in new[] { Login, Register, ChangePassword })
            {
                // O IP vem do RemoteIpAddress; com Proxy:ConfiarNoProxy=true, o UseForwardedHeaders o troca pelo X-Forwarded-For.
                options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "sem-ip",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
            }
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value) ? value : window;
                SetRetryAfter(context.HttpContext.Response, retryAfter);
                await context.HttpContext.Response.WriteAsJsonAsync(new { message = TooManyAttemptsMessage }, cancellationToken);
            };
        });
    }

    public static void SetRetryAfter(HttpResponse response, TimeSpan retryAfter) =>
        response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
}

// Balde por condomínio (janela fixa), independente do IP de quem tenta.
public sealed class CondominiumAttemptLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;
    private readonly TimeSpan _window;

    public CondominiumAttemptLimiter(int permitLimit, TimeSpan window)
    {
        _window = window;
        _limiter = PartitionedRateLimiter.Create<string, string>(condominiumId => RateLimitPartition.GetFixedWindowLimiter(
            condominiumId,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
    }

    public bool TryAcquire(string? condominiumId, out TimeSpan retryAfter)
    {
        using var lease = _limiter.AttemptAcquire((condominiumId ?? string.Empty).Trim());
        retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var value) ? value : _window;
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
