using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GestaoPro.API.Services.Demo;

// Modo demonstração: confere a época do token no momento de gravar, depois de o corpo ser lido (uma escrita lenta pode
// ter começado antes do reset). Época antiga: 401 (o painel e o app voltam ao login). Reset em andamento: 503.
public sealed class DemoEpochWriteFilter : IAsyncActionFilter
{
    public const string StaleSessionMessage = "Sessão da demonstração encerrada: os dados foram restaurados. Entre de novo.";

    private readonly DemoCatalog _catalog;
    private readonly DemoResetGate _gate;

    public DemoEpochWriteFilter(DemoCatalog catalog, DemoResetGate gate)
    {
        _catalog = catalog;
        _gate = gate;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        var isWrite = !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));
        if (_catalog.Enabled && isWrite)
        {
            if (_gate.Resetting)
            {
                context.HttpContext.Response.Headers.RetryAfter = Middleware.DemoResetGateMiddleware.RetryAfterSeconds.ToString();
                context.Result = new ObjectResult(new { message = Middleware.DemoResetGateMiddleware.ResettingMessage }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                return;
            }
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true && !_catalog.IsCurrentEpoch(user.FindFirstValue(DemoCatalog.EpochClaim)))
            {
                context.Result = new ObjectResult(new { message = StaleSessionMessage }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }
        }
        await next();
    }
}
