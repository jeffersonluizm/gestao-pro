using System.Security.Claims;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Tests.Access;

// Monta o usuário logado nos testes de controller com as mesmas claims que o TokenService emite.
public static class TestSession
{
    public const string CondominioA = "665f1c2e9b1e8a00000000a1";
    public const string CondominioB = "665f1c2e9b1e8a00000000b1";

    public static T As<T>(this T controller, string userId, string role, string? condominiumId = CondominioA)
        where T : ControllerBase
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
        };
        if (condominiumId is not null) claims.Add(new Claim(Caller.CondominiumIdClaim, condominiumId));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) }
        };
        return controller;
    }

    public static int StatusOf(IActionResult result) => result switch
    {
        ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
        StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
        _ => throw new InvalidOperationException($"Resultado inesperado: {result.GetType().Name}"),
    };

    public static int StatusOf<T>(ActionResult<T> result) => StatusOf(result.Result ?? new OkObjectResult(result.Value));

    public static TValue ValueOf<TValue>(IActionResult result) =>
        Assert.IsAssignableFrom<TValue>(Assert.IsAssignableFrom<ObjectResult>(result).Value);

    public static TValue ValueOf<T, TValue>(ActionResult<T> result) =>
        Assert.IsAssignableFrom<TValue>(Assert.IsAssignableFrom<ObjectResult>(result.Result).Value);
}
