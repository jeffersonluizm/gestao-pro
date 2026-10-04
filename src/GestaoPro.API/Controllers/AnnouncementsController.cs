using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/announcements")]
[Produces("application/json")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _service;

    public AnnouncementsController(IAnnouncementService service)
    {
        _service = service;
    }

    /// <summary>Lista os avisos do condomínio de quem está logado (paginação opcional: page, pageSize até 100)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? pageSize = null) =>
        ListAsync(activeOnly: false, category: null, page, pageSize);

    /// <summary>Lista apenas os avisos ativos do condomínio</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetActive([FromQuery] int? page = null, [FromQuery] int? pageSize = null) =>
        ListAsync(activeOnly: true, category: null, page, pageSize);

    // A consulta já vem filtrada pelo condomínio do token (no banco, com índice); o total vai em X-Total-Count.
    private async Task<IActionResult> ListAsync(bool activeOnly, string? category, int? page, int? pageSize)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (Paging.Normalize(page, pageSize) is not var (normalizedPage, normalizedSize))
            return BadRequest(new { message = $"Página inválida: page a partir de 1 e pageSize de 1 a {Paging.MaxPageSize}." });

        var result = await _service.QueryAsync(new AnnouncementQuery(
            caller.CondominiumId!, activeOnly, category, normalizedPage, normalizedSize));
        Response.Headers["X-Total-Count"] = result.Total.ToString();
        return Ok(result.Items);
    }

    /// <summary>Busca um aviso pelo ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var announcement = await _service.GetByIdAsync(id);
        if (announcement is null) return NotFound();
        return this.DenyOtherCondominium(this.Caller(), announcement.CondominiumId) ?? Ok(announcement);
    }

    /// <summary>Lista avisos por condomÃ­nio</summary>
    [HttpGet("condominium/{condominiumId}")]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        if (this.DenyOtherCondominium(this.Caller(), condominiumId) is { } denied) return denied;

        var announcements = await _service.GetByCondominiumAsync(condominiumId);
        return Ok(announcements);
    }

    /// <summary>Filtra avisos por categoria</summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetByCategory(string category, [FromQuery] int? page = null, [FromQuery] int? pageSize = null) =>
        ListAsync(activeOnly: false, category, page, pageSize);

    /// <summary>Cria um novo aviso ou comunicado</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var caller = this.Caller();
        if (this.DenyManage(caller, dto.CondominiumId, caller.IsAdministrator) is { } denied) return denied;

        dto.AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? dto.AuthorId;
        if (string.IsNullOrWhiteSpace(dto.AuthorId))
            return BadRequest(new { message = "Login obrigatório para identificar o autor do aviso." });

        try
        {
            var announcement = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = announcement.Id }, announcement);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Atualiza um aviso existente</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAnnouncementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await DenyManageAsync(id) is { } denied) return denied;

        var announcement = await _service.UpdateAsync(id, dto);
        return announcement is null ? NotFound() : Ok(announcement);
    }

    /// <summary>Remove um aviso</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // Escrita da administração só no próprio condomínio; aviso de outro condomínio responde 404.
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

