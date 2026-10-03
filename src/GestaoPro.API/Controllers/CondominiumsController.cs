using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CondominiumsController : ControllerBase
{
    private readonly CondominiumAppService _service;

    public CondominiumsController(CondominiumAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
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

