using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IPetService _service;

    public PetsController(IPetService service)
    {
        _service = service;
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetAllByCondominium(string condominiumId) =>
        Ok(await _service.GetAllByCondominiumAsync(condominiumId));

    [HttpGet("unit/{unitId}")]
    public async Task<IActionResult> GetByUnit(string unitId) =>
        Ok(await _service.GetByUnitAsync(unitId));

    [HttpGet("owner/{ownerId}")]
    public async Task<IActionResult> GetByOwner(string ownerId) =>
        Ok(await _service.GetByOwnerAsync(ownerId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var pet = await _service.GetByIdAsync(id);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest request)
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
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePetRequest request)
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
}

