using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Controllers;

// Ocorrência é do morador que registrou: ele vê e remove as próprias; a administração vê e trata as do condomínio.
// Quem chama é sempre o usuário do token. Os cabeçalhos X-User-Id/X-Condominium-Id só são lidos quando a
// administração registra em nome de um morador, e esse morador precisa ser do próprio condomínio.
[ApiController]
[Route("api/[controller]")]
public class OccurrencesController : ControllerBase
{
    private readonly IOccurrenceService _service;
    private readonly IUserDirectory _users;

    public OccurrencesController(IOccurrenceService service, IUserDirectory users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetAllByCondominium(string condominiumId)
    {
        var caller = this.Caller();
        if (DenyCondominium(caller, condominiumId) is { } denied) return denied;

        return Ok(Visible(caller, await _service.GetAllByCondominiumAsync(condominiumId)));
    }

    [HttpGet("condominium/{condominiumId}/status/{status}")]
    public async Task<IActionResult> GetByCondominiumAndStatus(
        string condominiumId, OccurrenceStatus status)
    {
        var caller = this.Caller();
        if (DenyCondominium(caller, condominiumId) is { } denied) return denied;

        return Ok(Visible(caller, await _service.GetByCondominiumAndStatusAsync(condominiumId, status)));
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!caller.IsAdministrator && !string.Equals(caller.UserId, userId, StringComparison.Ordinal))
            return this.Forbidden();

        return Ok(Visible(caller, await _service.GetByUserAsync(userId)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var occurrence = await _service.GetByIdAsync(id);
        if (occurrence is null) return NotFound();

        return this.Deny(ResourceAccess.Decide(caller, occurrence.UserId, occurrence.CondominiumId, caller.IsAdministrator))
            ?? Ok(occurrence);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOccurrenceRequest request)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        if (caller.IsAdministrator)
        {
            // A administração registra para um morador (o painel manda o morador em X-User-Id), só no próprio condomínio.
            var userId = FirstFilled(Request.Headers["X-User-Id"].ToString(), request.UserId);
            var condominiumId = FirstFilled(Request.Headers["X-Condominium-Id"].ToString(), request.CondominiumId);
            if (this.DenyManage(caller, condominiumId, staff: true) is { } denied) return denied;
            userId ??= caller.UserId!;
            var resident = await _users.GetByIdAsync(userId);
            if (this.DenyManage(caller, resident?.CondominiumId, staff: true) is { } otherResident) return otherResident;
            request = request with { UserId = userId, CondominiumId = condominiumId! };
        }
        else
        {
            request = request with { UserId = caller.UserId!, CondominiumId = caller.CondominiumId! };
        }

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
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOccurrenceRequest request)
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

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        string id, [FromBody] UpdateOccurrenceStatusRequest request)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        // Quem mudou o status é sempre o usuário do token.
        request = request with { UpdatedByUserId = this.Caller().UserId! };

        try
        {
            var updated = await _service.UpdateStatusAsync(id, request);
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
        // O morador remove as próprias; a administração, as do condomínio.
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var occurrence = await _service.GetByIdAsync(id);
        if (occurrence is null) return NotFound();
        if (this.Deny(ResourceAccess.Decide(caller, occurrence.UserId, occurrence.CondominiumId, caller.IsAdministrator)) is { } denied)
            return denied;

        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    private ActionResult? DenyCondominium(Caller caller, string condominiumId)
    {
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        return string.Equals(caller.CondominiumId, condominiumId, StringComparison.Ordinal) ? null : NotFound();
    }

    private async Task<ActionResult?> DenyManageAsync(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        if (!caller.IsAdministrator) return this.Forbidden();

        var occurrence = await _service.GetByIdAsync(id);
        if (occurrence is null) return NotFound();
        return this.DenyManage(caller, occurrence.CondominiumId, staff: true);
    }

    private static List<OccurrenceResponse> Visible(Caller caller, IEnumerable<OccurrenceResponse> occurrences) =>
        occurrences
            .Where(occurrence => ResourceAccess.CanSee(caller, occurrence.UserId, occurrence.CondominiumId, caller.IsAdministrator))
            .ToList();

    private static string? FirstFilled(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
