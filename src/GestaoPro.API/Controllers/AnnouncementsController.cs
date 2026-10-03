using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
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

    /// <summary>Lista todos os avisos e comunicados</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var announcements = await _service.GetAllAsync();
        return Ok(announcements);
    }

    /// <summary>Lista apenas os avisos ativos</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        var announcements = await _service.GetActiveAsync();
        return Ok(announcements);
    }

    /// <summary>Busca um aviso pelo ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var announcement = await _service.GetByIdAsync(id);
        return announcement is null ? NotFound() : Ok(announcement);
    }

    /// <summary>Lista avisos por condomÃ­nio</summary>
    [HttpGet("condominium/{condominiumId}")]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        var announcements = await _service.GetByCondominiumAsync(condominiumId);
        return Ok(announcements);
    }

    /// <summary>Filtra avisos por categoria</summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(List<Announcement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var announcements = await _service.GetByCategoryAsync(category);
        return Ok(announcements);
    }

    /// <summary>Cria um novo aviso ou comunicado</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

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

        var announcement = await _service.UpdateAsync(id, dto);
        return announcement is null ? NotFound() : Ok(announcement);
    }

    /// <summary>Remove um aviso</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

