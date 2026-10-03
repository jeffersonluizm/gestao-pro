using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CommonAreasController : ControllerBase
{
    private readonly ICommonAreaService _service;

    public CommonAreasController(ICommonAreaService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CommonArea>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var commonAreas = await _service.GetAllAsync();
        return Ok(commonAreas);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CommonArea), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var commonArea = await _service.GetByIdAsync(id);
        return commonArea is null ? NotFound("Área comum não encontrada.") : Ok(commonArea);
    }

    [HttpGet("condominium/{condominiumId}")]
    [ProducesResponseType(typeof(List<CommonArea>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        var commonAreas = await _service.GetByCondominiumAsync(condominiumId);
        return Ok(commonAreas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommonArea), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCommonAreaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var commonArea = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = commonArea.Id }, commonArea);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CommonArea), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCommonAreaDto dto)
    {
        var commonArea = await _service.UpdateAsync(id, dto);
        return commonArea is null ? NotFound("Área comum não encontrada.") : Ok(commonArea);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound("Área comum não encontrada.");
    }
}
