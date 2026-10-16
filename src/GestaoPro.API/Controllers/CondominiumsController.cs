using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CondominiumsController : ControllerBase
{
    private readonly ICondominiumService _service;
    private readonly DemoGuard _demo;

    public CondominiumsController(ICondominiumService service, DemoCatalog? demo = null)
    {
        _service = service;
        _demo = new DemoGuard(demo);
    }

    // Com login: o condomínio do próprio token (lista de um item, para as telas de gestão).
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var own = await _service.GetByIdAsync(caller.CondominiumId!);
        return Ok(own is null ? new List<CondominiumResponse>() : new List<CondominiumResponse> { own });
    }

    // Sem login (tela de cadastro): só id e nome de cada condomínio.
    [HttpGet("public")]
    public async Task<IActionResult> GetPublic() =>
        Ok(await _service.GetPublicAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        // O detalhe é só do próprio condomínio.
        if (this.DenyOtherCondominium(this.Caller(), id) is { } denied) return denied;

        var condominium = await _service.GetByIdAsync(id);
        return condominium is null ? NotFound() : Ok(condominium);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCondominiumRequest request)
    {
        // Na demonstração pública, um condomínio novo apareceria na lista do cadastro para todo mundo.
        if (_demo.Enabled) return DemoGuard.Forbidden(DemoGuard.NoNewCondominiumMessage);

        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCondominiumRequest request)
    {
        if (DenyOtherCondominium(id) is { } denied) return denied;
        // Condomínio da demonstração: regras e endereço podem mudar; nome e código, não.
        if (_demo.IsProtectedCondominium(id) && (request.Name is not null || !string.IsNullOrWhiteSpace(request.DefaultPassword)))
            return DemoGuard.Forbidden(DemoGuard.ProtectedCondominiumMessage);

        try
        {
            var updated = await _service.UpdateAsync(id, request);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (DenyOtherCondominium(id) is { } denied) return denied;
        if (_demo.IsProtectedCondominium(id)) return DemoGuard.Forbidden(DemoGuard.ProtectedCondominiumMessage);

        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // A administração altera e remove só o condomínio do próprio token. Criar um condomínio novo segue liberado
    // (o cadastro inicial acontece antes de existir administrador; ver ApiAccessMiddleware).
    private ActionResult? DenyOtherCondominium(string id)
    {
        var caller = this.Caller();
        return this.DenyManage(caller, id, caller.IsAdministrator);
    }
}

