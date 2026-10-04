using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Controllers;

// Pet é da unidade: o morador vê os próprios e os da sua unidade; a administração vê e altera os do condomínio.
[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IPetService _service;
    private readonly IUserDirectory _users;

    public PetsController(IPetService service, IUserDirectory users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetAllByCondominium(string condominiumId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!string.Equals(caller.CondominiumId, condominiumId, StringComparison.Ordinal)) return NotFound();

        return Ok(await VisibleAsync(caller, await _service.GetAllByCondominiumAsync(condominiumId)));
    }

    [HttpGet("unit/{unitId}")]
    public async Task<IActionResult> GetByUnit(string unitId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        return Ok(await VisibleAsync(caller, await _service.GetByUnitAsync(unitId)));
    }

    [HttpGet("owner/{ownerId}")]
    public async Task<IActionResult> GetByOwner(string ownerId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!caller.IsAdministrator && !string.Equals(caller.UserId, ownerId, StringComparison.Ordinal))
            return this.Forbidden();

        return Ok(await VisibleAsync(caller, await _service.GetByOwnerAsync(ownerId)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var pet = await _service.GetByIdAsync(id);
        if (pet is null) return NotFound();

        var decision = ResourceAccess.Decide(caller, pet.OwnerId, pet.CondominiumId, caller.IsAdministrator);
        if (decision == AccessDecision.Forbidden && IsCallerUnit(pet, await CallerUnitIdAsync(caller)))
            decision = AccessDecision.Allowed;

        return this.Deny(decision) ?? Ok(pet);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest request)
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
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePetRequest request)
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

    private async Task<ActionResult?> DenyManageAsync(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        if (!caller.IsAdministrator) return this.Forbidden();

        var pet = await _service.GetByIdAsync(id);
        if (pet is null) return NotFound();
        return this.DenyManage(caller, pet.CondominiumId, staff: true);
    }

    private async Task<List<PetResponse>> VisibleAsync(Caller caller, IEnumerable<PetResponse> pets)
    {
        var callerUnitId = caller.IsAdministrator ? null : await CallerUnitIdAsync(caller);
        return pets
            .Where(pet =>
                ResourceAccess.CanSee(caller, pet.OwnerId, pet.CondominiumId, caller.IsAdministrator) ||
                (string.Equals(pet.CondominiumId, caller.CondominiumId, StringComparison.Ordinal) && IsCallerUnit(pet, callerUnitId)))
            .ToList();
    }

    // Unidade do morador logado (cadastro do usuário), para mostrar os pets de quem mora com ele.
    private async Task<string?> CallerUnitIdAsync(Caller caller) =>
        (await _users.GetByIdAsync(caller.UserId!))?.UnitId;

    private static bool IsCallerUnit(PetResponse pet, string? callerUnitId) =>
        !string.IsNullOrWhiteSpace(callerUnitId) && string.Equals(pet.UnitId, callerUnitId, StringComparison.Ordinal);
}
