namespace GestaoPro.API.Middleware;

// Tempo máximo das escritas em /api (Limites:TimeoutEscritaSegundos, padrão 15 s): o reset da demonstração espera
// 10 s pelas escritas em andamento, e uma escrita lenta não pode segurar a conexão. Leituras (listas, relatórios) e o
// hub de notificações (conexão longa) ficam de fora. Passou do tempo: a requisição é cancelada e responde 504 com
// mensagem; se o código não atender o cancelamento em 2 s (por exemplo, leitura de corpo que não o observa), a conexão
// é abortada.
public sealed class WriteTimeoutMiddleware
{
    public const string TimeoutMessage = "A operação demorou demais e foi cancelada. Tente de novo.";

    public static readonly TimeSpan AbortGrace = TimeSpan.FromSeconds(2);

    private readonly RequestDelegate _next;
    private readonly TimeSpan _timeout;

    public WriteTimeoutMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _timeout = TimeSpan.FromSeconds(configuration.GetValue("Limites:TimeoutEscritaSegundos", 15));
    }

    public static bool AppliesTo(string method, PathString path) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)) &&
        path.StartsWithSegments("/api");

    public async Task InvokeAsync(HttpContext context)
    {
        if (!AppliesTo(context.Request.Method, context.Request.Path))
        {
            await _next(context);
            return;
        }

        var original = context.RequestAborted;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(original);
        using var abort = new CancellationTokenSource();
        timeout.CancelAfter(_timeout);
        abort.CancelAfter(_timeout + AbortGrace);
        using var abortRegistration = abort.Token.Register(context.Abort);
        context.RequestAborted = timeout.Token;
        try
        {
            await _next(context);
        }
        catch (Exception error) when (timeout.IsCancellationRequested && !original.IsCancellationRequested &&
                                      !abort.IsCancellationRequested && !context.Response.HasStarted && IsCancellation(error))
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await context.Response.WriteAsJsonAsync(new { message = TimeoutMessage }, CancellationToken.None);
        }
        finally
        {
            context.RequestAborted = original;
        }
    }

    // O cancelamento pode chegar embrulhado (leitura do corpo, driver do banco).
    private static bool IsCancellation(Exception? error)
    {
        for (; error is not null; error = error.InnerException)
            if (error is OperationCanceledException or IOException) return true;
        return false;
    }
}
