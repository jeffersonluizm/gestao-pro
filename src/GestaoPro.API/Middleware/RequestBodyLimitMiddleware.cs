using Microsoft.AspNetCore.Http.Features;

namespace GestaoPro.API.Middleware;

// Teto do corpo da requisição em toda a API (padrão 64 KB; nada aqui envia arquivo). O Kestrel aplica o mesmo
// limite (Program.cs); este middleware responde 413 com mensagem legível antes de o corpo ser lido.
public sealed class RequestBodyLimitMiddleware
{
    public const long DefaultMaxBytes = 64 * 1024;
    public const string TooLargeMessage = "Os dados enviados são grandes demais. Encurte os textos e tente de novo.";

    private readonly RequestDelegate _next;
    private readonly long _maxBytes;

    public RequestBodyLimitMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _maxBytes = MaxBytes(configuration);
    }

    public static long MaxBytes(IConfiguration configuration) =>
        Math.Max(1024, configuration.GetValue("Limites:CorpoMaximoBytes", DefaultMaxBytes));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _maxBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { message = TooLargeMessage });
            return;
        }

        // Corpo sem Content-Length (chunked): o servidor corta no mesmo limite.
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false }) feature.MaxRequestBodySize = _maxBytes;

        await _next(context);
    }
}
