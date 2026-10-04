using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CondominiumsController : ControllerBase
{
    private readonly ICondominiumService _service;

    public CondominiumsController(ICondominiumService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        // A lista (GET /api/condominiums) segue pública para a tela de cadastro; o detalhe é só do próprio condomínio.
        if (this.DenyOtherCondominium(this.Caller(), id) is { } denied) return denied;

        var condominium = await _service.GetByIdAsync(id);
        return condominium is null ? NotFound() : Ok(condominium);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCondominiumRequest request)
    {
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

