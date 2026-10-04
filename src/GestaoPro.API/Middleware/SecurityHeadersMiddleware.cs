namespace GestaoPro.API.Middleware;

// Cabeçalhos de segurança em toda resposta. A API só devolve JSON: a CSP não libera nada, exceto
// na interface do Swagger (que só existe quando habilitada), que precisa dos próprios scripts e estilos.
public class SecurityHeadersMiddleware
{
    private const string ApiPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    private const string SwaggerPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; frame-ancestors 'none'; base-uri 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] =
            context.Request.Path.StartsWithSegments("/swagger") ? SwaggerPolicy : ApiPolicy;
        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        return _next(context);
    }
}
