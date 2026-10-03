using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly UnitAppService _service;

    public UnitsController(UnitAppService service)
    {
        _service = service;
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetAllByCondominium(string condominiumId) =>
        Ok(await _service.GetAllByCondominiumAsync(condominiumId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var unit = await _service.GetByIdAsync(id);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request)
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
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUnitRequest request)
    {
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
        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id}/residents/{residentId}")]
    public async Task<IActionResult> AddResident(string id, string residentId)
    {
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
}

