using GestaoPro.API.Services.Demo;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

public sealed record DemoAccountResponse(string Role, string Name, string Email, string Password);

public sealed record DemoInfoResponse(string CondominiumName, string CondominiumCode, List<DemoAccountResponse> Accounts);

// GET /api/demo: fonte única das credenciais de demonstração (painel e app leem daqui).
// Só existe com Demo:Enabled=true; desligado, responde 404 como uma rota inexistente.
[ApiController]
[Route("api/demo")]
public class DemoController : ControllerBase
{
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
            _catalog.Accounts.Select(a => new DemoAccountResponse(a.Role, a.Name, a.Email, a.Password)).ToList()));
    }
}
