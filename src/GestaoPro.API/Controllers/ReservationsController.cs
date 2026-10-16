using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/reservations")]
[Produces("application/json")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _service;
    private readonly ICommonAreaService _commonAreas;
    private readonly IUserDirectory _users;

    public ReservationsController(IReservationService service, ICommonAreaService commonAreas, IUserDirectory users)
    {
        _service = service;
        _commonAreas = commonAreas;
        _users = users;
    }

    /// <summary>Lista as reservas que o usuário pode ver (morador: as próprias; administração: as do condomínio)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var reservations = caller.IsAdministrator
            ? await _service.GetByCondominiumAsync(caller.CondominiumId!)
            : await _service.GetByUserAsync(caller.UserId!);
        return Ok(Visible(caller, reservations));
    }

    /// <summary>Busca uma reserva pelo ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Reservation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var reservation = await _service.GetByIdAsync(id);
        if (reservation is null) return NotFound();

        return this.Deny(ResourceAccess.Decide(caller, reservation.UserId, reservation.CondominiumId, caller.IsAdministrator))
            ?? Ok(reservation);
    }

    /// <summary>Lista reservas por condomínio (só o próprio; morador vê as próprias)</summary>
    [HttpGet("condominium/{condominiumId}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!string.Equals(caller.CondominiumId, condominiumId, StringComparison.Ordinal)) return NotFound();

        var reservations = await _service.GetByCondominiumAsync(condominiumId);
        return Ok(Visible(caller, reservations));
    }

    /// <summary>Lista reservas por usuário</summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!caller.IsAdministrator && !string.Equals(caller.UserId, userId, StringComparison.Ordinal))
            return this.Forbidden();

        var reservations = await _service.GetByUserAsync(userId);
        return Ok(Visible(caller, reservations));
    }

    /// <summary>Lista reservas por área comum</summary>
    [HttpGet("common-area/{commonAreaId}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCommonArea(string commonAreaId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var reservations = await _service.GetByCommonAreaAsync(commonAreaId);
        return Ok(Visible(caller, reservations));
    }

    /// <summary>Filtra reservas por status (pendente, aprovada, rejeitada, cancelada)</summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var reservations = await _service.GetByStatusAsync(status);
        return Ok(Visible(caller, reservations));
    }

    /// <summary>Lista reservas por data (formato: yyyy-MM-dd)</summary>
    [HttpGet("date/{date}")]
    [ProducesResponseType(typeof(List<Reservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDate(string date)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var reservations = await _service.GetByDateAsync(date);
        return Ok(Visible(caller, reservations));
    }

    /// <summary>Cria uma nova reserva</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Reservation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [DemoQuota("reservations")]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

            // A área comum define o condomínio da reserva: área de outro condomínio (ou inexistente) responde 404,
            // sem chegar à checagem de conflito, que revelaria a ocupação e bloquearia a área do outro condomínio.
            var commonArea = await _commonAreas.GetByIdAsync(dto.CommonAreaId);
            if (this.DenyOtherCondominium(caller, commonArea?.CondominiumId) is { } otherArea) return otherArea;
            dto.CondominiumId = commonArea!.CondominiumId;

            if (caller.IsAdministrator)
            {
                // Admin/síndico reserva para morador do próprio condomínio.
                var resident = await _users.GetByIdAsync(dto.UserId);
                if (this.DenyManage(caller, resident?.CondominiumId, staff: true) is { } denied) return denied;
            }
            else
            {
                // Quem não administra reserva em nome próprio.
                dto.UserId = caller.UserId!;
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

        // Aprovar, rejeitar ou alterar: só a administração do condomínio da reserva (outro: 404).
        var caller = this.Caller();
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        if (this.DenyManage(caller, existing.CondominiumId, caller.IsAdministrator) is { } denied) return denied;

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
        // O morador remove as próprias reservas (app mobile); a administração, as do condomínio.
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var reservation = await _service.GetByIdAsync(id);
        if (reservation is null) return NotFound();
        if (this.Deny(ResourceAccess.Decide(caller, reservation.UserId, reservation.CondominiumId, caller.IsAdministrator)) is { } denied)
            return denied;

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // Filtro das listas: morador fica com as próprias, administração com as do condomínio.
    private static List<Reservation> Visible(Caller caller, IEnumerable<Reservation> reservations) =>
        reservations
            .Where(reservation => ResourceAccess.CanSee(caller, reservation.UserId, reservation.CondominiumId, caller.IsAdministrator))
            .ToList();
}

