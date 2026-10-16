using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Middleware;

// Modo demonstração: enquanto o reset roda, toda escrita em /api (inclusive login e entrada na demo) responde 503 com
// Retry-After; fora do reset, conta as escritas em andamento para o reset esperar por elas antes de limpar.
public sealed class DemoResetGateMiddleware
{
    public const string ResettingMessage = "A demonstração está voltando ao estado inicial. Tente de novo em alguns segundos.";
    public const int RetryAfterSeconds = 15;

    private readonly RequestDelegate _next;
    private readonly DemoResetGate _gate;
    private readonly DemoCatalog _catalog;

    public DemoResetGateMiddleware(RequestDelegate next, DemoResetGate gate, DemoCatalog catalog)
    {
        _next = next;
        _gate = gate;
        _catalog = catalog;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var isWrite = !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));
        if (!_catalog.Enabled || !isWrite || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (!_gate.TryEnterWrite())
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new { message = ResettingMessage });
            return;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            _gate.ExitWrite();
        }
    }
}
