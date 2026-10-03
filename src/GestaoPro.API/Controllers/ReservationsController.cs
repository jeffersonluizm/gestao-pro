using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/reservations")]
[Produces("application/json")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _service;

    public ReservationsController(IReservationService service)
    {
        _service = service;
    }

    /// <summary>Lista todas as reservas</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdministrator() && CurrentUserId() is { Length: > 0 } currentUserId)
        {
            var userReservations = await _service.GetByUserAsync(currentUserId);
            return Ok(userReservations);
        }

        var reservations = await _service.GetAllAsync();
        return Ok(reservations);
    }

    /// <summary>Busca uma reserva pelo ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Reservation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var reservation = await _service.GetByIdAsync(id);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    /// <summary>Lista reservas por condomÃ­nio</summary>
    [HttpGet("condominium/{condominiumId}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        var reservations = await _service.GetByCondominiumAsync(condominiumId);
        return Ok(reservations);
    }

    /// <summary>Lista reservas por usuÃ¡rio</summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(string userId)
    {
        if (!IsAdministrator() && !string.Equals(CurrentUserId(), userId, StringComparison.Ordinal))
            return Forbid();

        var reservations = await _service.GetByUserAsync(userId);
        return Ok(reservations);
    }

    /// <summary>Lista reservas por Ã¡rea comum</summary>
    [HttpGet("common-area/{commonAreaId}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCommonArea(string commonAreaId)
    {
        var reservations = await _service.GetByCommonAreaAsync(commonAreaId);
        return Ok(reservations);
    }

    /// <summary>Filtra reservas por status (pendente, aprovada, rejeitada, cancelada)</summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var reservations = await _service.GetByStatusAsync(status);
        return Ok(reservations);
    }

    /// <summary>Lista reservas por data (formato: yyyy-MM-dd)</summary>
    [HttpGet("date/{date}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDate(string date)
    {
        var reservations = await _service.GetByDateAsync(date);
        return Ok(reservations);
    }

    /// <summary>Cria uma nova reserva</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Reservation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            if (!IsAdministrator())
            {
                var currentUserId = CurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return Unauthorized(new { message = "Login obrigatório para criar reserva." });

                dto.UserId = currentUserId;
            }

            var reservation = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Atualiza uma reserva existente</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Reservation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateReservationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var reservation = await _service.UpdateAsync(id, dto);
            return reservation is null ? NotFound() : Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Remove uma reserva</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool IsAdministrator()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return role is "admin" or "sindico";
    }
}

