using GestaoPro.API.Services.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestaoPro.API.Controllers;

public sealed record DemoAccountResponse(string Role, string Name);

public sealed record DemoResidentResponse(string Cpf, string Name);

public sealed record DemoInfoResponse(
    string CondominiumName, string CondominiumCode, List<DemoAccountResponse> Accounts, List<DemoResidentResponse> PreRegistered);

public sealed record DemoEnterRequest(string? Papel);

// Modo demonstração. Só existe com Demo:Enabled=true; desligado, responde 404 como uma rota inexistente.
// GET /api/demo: papéis, nomes e o código do condomínio (para demonstrar a ativação por CPF). Nenhuma senha.
// POST /api/demo/entrar: devolve a sessão pronta da conta demo do papel (sindico, morador ou porteiro).
[ApiController]
[Route("api/demo")]
public class DemoController : ControllerBase
{
    public const string NotReadyMessage = "A demonstração está sendo preparada. Tente de novo em alguns segundos.";
    public const string InvalidRoleMessage = "Escolha um dos papéis da demonstração: síndico, morador ou porteiro.";

    private readonly DemoCatalog _catalog;

    public DemoController(DemoCatalog catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public IActionResult Get()
    {
        if (!_catalog.Enabled) return NotFound();

        return Ok(new DemoInfoResponse(
            DemoCatalog.CondominiumName,
            _catalog.CondominiumCode,
            _catalog.Accounts.Select(a => new DemoAccountResponse(a.Role, a.Name)).ToList(),
            DemoCatalog.PreRegisteredResidents
                .Select(r => new DemoResidentResponse($"{r.Cpf[..3]}.{r.Cpf[3..6]}.{r.Cpf[6..9]}-{r.Cpf[9..]}", r.Name))
                .ToList()));
    }

    [HttpPost("entrar")]
    [EnableRateLimiting(AuthRateLimit.DemoEnter)]
    public Task<IActionResult> Enter([FromBody] DemoEnterRequest request) =>
        EnterCore(request, () => HttpContext.RequestServices.GetRequiredService<IDemoSessions>());

    [NonAction]
    public Task<IActionResult> Enter(DemoEnterRequest request, IDemoSessions sessions) => EnterCore(request, () => sessions);

    // O serviço de sessão (contas no banco) só é montado depois das conferências: sem a flag, 404 sem tocar no banco.
    private async Task<IActionResult> EnterCore(DemoEnterRequest request, Func<IDemoSessions> sessions)
    {
        if (!_catalog.Enabled) return NotFound();

        var account = _catalog.AccountFor(request.Papel);
        if (account is null) return BadRequest(new { message = InvalidRoleMessage });

        var session = await sessions().EnterAsync(account.Role);
        return session is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = NotReadyMessage })
            : Ok(session);
    }
}
