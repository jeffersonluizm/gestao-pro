using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        private readonly ParkingAppService _parkingService;

        public ParkingController(ParkingAppService parkingService)
        {
            _parkingService = parkingService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Parking>>> Get()
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

            return Ok(Visible(caller, await _parkingService.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Parking>> GetById(string id)
        {
            var parking = await _parkingService.GetByIdAsync(id);

            if (parking == null)
                return NotFound();

            var caller = this.Caller();
            return this.DenyOtherCondominium(caller, parking.CondominiumId) ?? Ok(ForCaller(caller, parking));
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<Parking>>> GetAvailable()
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

            return Ok(Visible(caller, await _parkingService.GetAvailableAsync()));
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateParkingDto dto)
        {
            // Sem condomínio no cadastro (o painel não manda), vale o do token; outro condomínio: 404.
            var caller = this.Caller();
            if (string.IsNullOrWhiteSpace(dto.CondominiumId)) dto.CondominiumId = caller.CondominiumId;
            if (this.DenyManage(caller, dto.CondominiumId, caller.IsAdministrator) is { } denied) return denied;

            try
            {
                var parking = await _parkingService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = parking.Id }, parking);
            }
            catch (InvalidOperationException ex)
            {
                // Número de vaga repetido (ParkingAppService): conflito, não erro do servidor.
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("reserve/{id}")]
        public async Task<ActionResult> Reserve(string id)
        {
            if (await DenyManageAsync(id) is { } denied) return denied;

            var userId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _parkingService.ReserveAsync(id, userId);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Parking>> Update(string id, UpdateParkingDto dto)
        {
            if (await DenyManageAsync(id) is { } denied) return denied;

            // Não move a vaga para outro condomínio.
            if (dto.CondominiumId is not null && this.DenyManage(this.Caller(), dto.CondominiumId, staff: true) is { } moved)
                return moved;

            try
            {
                var parking = await _parkingService.UpdateAsync(id, dto);
                return parking is null ? NotFound() : Ok(parking);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateStatus(string id, [FromQuery] string status)
        {
            if (await DenyManageAsync(id) is { } denied) return denied;

            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("O status Ã© obrigatÃ³rio.");

            await _parkingService.UpdateStatusAsync(id, status);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            if (await DenyManageAsync(id) is { } denied) return denied;

            await _parkingService.DeleteAsync(id);
            return NoContent();
        }

        // Vagas do condomínio do token; quem não administra não recebe quem usa ou reservou as vagas dos outros.
        private static List<Parking> Visible(Caller caller, IEnumerable<Parking> parkings) =>
            caller.InCondominium(parkings, parking => parking.CondominiumId)
                .Select(parking => ForCaller(caller, parking))
                .ToList();

        private static Parking ForCaller(Caller caller, Parking parking)
        {
            if (caller.IsAdministrator) return parking;

            string? OnlyOwn(string? userId) =>
                string.Equals(userId, caller.UserId, StringComparison.Ordinal) ? userId : null;

            return new Parking
            {
                Id = parking.Id,
                Number = parking.Number,
                Type = parking.Type,
                CondominiumId = parking.CondominiumId,
                Status = parking.Status,
                IsShared = parking.IsShared,
                ResidentId = OnlyOwn(parking.ResidentId),
                UnitId = parking.UnitId,
                ReservedByUserId = OnlyOwn(parking.ReservedByUserId),
                ReservedAt = parking.ReservedAt,
                CreatedAt = parking.CreatedAt,
                History = parking.History
                    .Select(entry => new ParkingHistory
                    {
                        Action = entry.Action,
                        Status = entry.Status,
                        UserId = OnlyOwn(entry.UserId),
                        Date = entry.Date,
                    })
                    .ToList(),
            };
        }

        // Escrita da administração só em vaga do próprio condomínio (outro ou sem condomínio gravado: 404).
        private async Task<ActionResult?> DenyManageAsync(string id)
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
            if (!caller.IsAdministrator) return this.Forbidden();

            var parking = await _parkingService.GetByIdAsync(id);
            if (parking is null) return NotFound();
            return this.DenyManage(caller, parking.CondominiumId, staff: true);
        }
    }
}
