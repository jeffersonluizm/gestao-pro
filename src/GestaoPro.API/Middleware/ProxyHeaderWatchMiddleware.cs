namespace GestaoPro.API.Middleware;

// Com Proxy:CabecalhoIp configurado, avisa uma vez no log se uma requisição chegar sem esse cabeçalho: o limite por IP
// passaria a usar o IP do proxy para todo mundo (ver docs/deploy.md, "Conferir o IP real").
public sealed class ProxyHeaderWatchMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyHeaderWatchMiddleware> _logger;
    private readonly string _header;
    private int _warned;

    public ProxyHeaderWatchMiddleware(RequestDelegate next, ILogger<ProxyHeaderWatchMiddleware> logger, string header)
    {
        _next = next;
        _logger = logger;
        _header = header;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(_header) && Interlocked.Exchange(ref _warned, 1) == 0)
            _logger.LogWarning("Proxy:CabecalhoIp={Header}, mas o cabeçalho {Header} não chegou numa requisição: o limite por IP pode estar usando o IP do proxy.", _header, _header);
        return _next(context);
    }
}
