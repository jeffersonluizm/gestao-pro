using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OccurrencesController : ControllerBase
{
    private readonly IOccurrenceService _service;

    public OccurrencesController(IOccurrenceService service)
    {
        _service = service;
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetAllByCondominium(string condominiumId) =>
        Ok(await _service.GetAllByCondominiumAsync(condominiumId));

    [HttpGet("condominium/{condominiumId}/status/{status}")]
    public async Task<IActionResult> GetByCondominiumAndStatus(
        string condominiumId, OccurrenceStatus status) =>
        Ok(await _service.GetByCondominiumAndStatusAsync(condominiumId, status));

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId) =>
        Ok(await _service.GetByUserAsync(userId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var occurrence = await _service.GetByIdAsync(id);
        return occurrence is null ? NotFound() : Ok(occurrence);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOccurrenceRequest request)
    {
        var userIdLogado = Request.Headers["X-User-Id"].ToString();
        var condominiumId = Request.Headers["X-Condominium-Id"].ToString();

        if (string.IsNullOrEmpty(userIdLogado)) return Unauthorized();
        if (string.IsNullOrEmpty(condominiumId)) return Unauthorized();

        // Sobrescreve os valores do body com os do token
        request = request with
        {
            UserId = userIdLogado,
            CondominiumId = condominiumId
        };

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
        var userIdLogado = Request.Headers["X-User-Id"].ToString();
        if (string.IsNullOrEmpty(userIdLogado)) return Unauthorized();

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
        var userIdLogado = Request.Headers["X-User-Id"].ToString();
        if (string.IsNullOrEmpty(userIdLogado)) return Unauthorized();

        // Sobrescreve o UpdatedByUserId com o do token
        request = request with { UpdatedByUserId = userIdLogado };

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
        // Identidade vem do token, não de cabeçalho: o morador remove as próprias; a administração, as do condomínio.
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var occurrence = await _service.GetByIdAsync(id);
        if (occurrence is null) return NotFound();
        if (this.Deny(ResourceAccess.Decide(caller, occurrence.UserId, occurrence.CondominiumId, caller.IsAdministrator)) is { } denied)
            return denied;

        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}

