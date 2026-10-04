using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
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
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        // Filtro no banco: só as áreas do condomínio do token.
        return Ok(await _service.GetByCondominiumAsync(caller.CondominiumId!));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CommonArea), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var commonArea = await _service.GetByIdAsync(id);
        if (commonArea is null) return NotFound("Área comum não encontrada.");
        // De outro condomínio: a mesma resposta de inexistente.
        return this.DenyOtherCondominium(this.Caller(), commonArea.CondominiumId) is null
            ? Ok(commonArea)
            : NotFound("Área comum não encontrada.");
    }

    [HttpGet("condominium/{condominiumId}")]
    [ProducesResponseType(typeof(List<CommonArea>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        if (this.DenyOtherCondominium(this.Caller(), condominiumId) is { } denied) return denied;

        var commonAreas = await _service.GetByCondominiumAsync(condominiumId);
        return Ok(commonAreas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommonArea), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCommonAreaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var caller = this.Caller();
        if (this.DenyManage(caller, dto.CondominiumId, caller.IsAdministrator) is { } denied) return denied;

        var commonArea = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = commonArea.Id }, commonArea);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CommonArea), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCommonAreaDto dto)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        var commonArea = await _service.UpdateAsync(id, dto);
        return commonArea is null ? NotFound("Área comum não encontrada.") : Ok(commonArea);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound("Área comum não encontrada.");
    }

    // Escrita da administração só no próprio condomínio; área de outro condomínio responde 404.
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
