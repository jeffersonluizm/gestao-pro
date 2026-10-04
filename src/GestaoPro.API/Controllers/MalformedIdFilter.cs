using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Bson;

namespace GestaoPro.API.Controllers;

// Ids de rota ("id", "userId", "condominiumId"...) são ObjectId do MongoDB. Valor malformado fazia o driver
// lançar FormatException (500); aqui ele vira 400 antes de chegar ao controller. Registrado para todos os controllers.
public class MalformedIdFilter : IActionFilter
{
    public const string Message = "Identificador inválido.";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var (key, value) in context.RouteData.Values)
        {
            if (!IsIdRouteValue(key)) continue;
            if (value is string text && ObjectId.TryParse(text, out _)) continue;

            context.Result = new BadRequestObjectResult(new { message = Message });
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static bool IsIdRouteValue(string key) =>
        key.Equals("id", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Id", StringComparison.Ordinal);
}
