using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _service;
    private readonly IUserDirectory _users;

    public UnitsController(IUnitService service, IUserDirectory users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetAllByCondominium(string condominiumId)
    {
        var caller = this.Caller();
        if (this.DenyOtherCondominium(caller, condominiumId) is { } denied) return denied;

        var units = await _service.GetAllByCondominiumAsync(condominiumId);
        return Ok(units.Select(unit => ForCaller(caller, unit)).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var unit = await _service.GetByIdAsync(id);
        if (unit is null) return NotFound();

        var caller = this.Caller();
        return this.DenyOtherCondominium(caller, unit.CondominiumId) ?? Ok(ForCaller(caller, unit));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request)
    {
        var caller = this.Caller();
        if (this.DenyManage(caller, request.CondominiumId, caller.IsAdministrator) is { } denied) return denied;

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
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUnitRequest request)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

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
        if (await DenyManageAsync(id) is { } denied) return denied;

        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id}/residents/{residentId}")]
    public async Task<IActionResult> AddResident(string id, string residentId)
    {
        if (await DenyResidentLinkAsync(id, residentId) is { } denied) return denied;

        try
        {
            var result = await _service.AddResidentAsync(id, residentId);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}/residents/{residentId}")]
    public async Task<IActionResult> RemoveResident(string id, string residentId)
    {
        if (await DenyResidentLinkAsync(id, residentId) is { } denied) return denied;

        try
        {
            var result = await _service.RemoveResidentAsync(id, residentId);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Quem não administra não recebe os ids dos moradores das unidades (só o próprio, se morar nela).
    private static UnitResponse ForCaller(Caller caller, UnitResponse unit) =>
        caller.IsAdministrator
            ? unit
            : unit with { ResidentIds = unit.ResidentIds.Where(id => string.Equals(id, caller.UserId, StringComparison.Ordinal)).ToList() };

    // Vínculo só entre unidade e morador do próprio condomínio.
    private async Task<ActionResult?> DenyResidentLinkAsync(string id, string residentId)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        var resident = await _users.GetByIdAsync(residentId);
        return this.DenyManage(this.Caller(), resident?.CondominiumId, staff: true);
    }
    // Escrita da administração só no próprio condomínio; unidade de outro condomínio responde 404.
    private async Task<ActionResult?> DenyManageAsync(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        if (!caller.IsAdministrator) return this.Forbidden();

        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        return this.DenyManage(caller, existing.CondominiumId, staff: true);
    }
}

