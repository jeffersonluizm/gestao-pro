using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace GestaoPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitorController : ControllerBase
    {
        private readonly VisitorAppService _visitorService;
        private readonly ILogger<VisitorController> _logger;

        public VisitorController(VisitorAppService visitorService, ILogger<VisitorController> logger)
        {
            _visitorService = visitorService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Visitor>>> Get()
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

            var visitors = SeesWholeCondominium(caller)
                ? await _visitorService.GetAllAsync()
                : await _visitorService.GetByResidentAsync(caller.UserId!);
            return Ok(Visible(caller, visitors));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Visitor>> GetById(string id)
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

            var visitor = await _visitorService.GetByIdAsync(id);

            if (visitor == null)
                return NotFound();

            return this.Deny(ResourceAccess.Decide(caller, visitor.ResidentId, visitor.CondominiumId, SeesWholeCondominium(caller)))
                ?? Ok(visitor);
        }

        [HttpGet("resident/{residentId}")]
        public async Task<ActionResult<List<Visitor>>> GetByResident(string residentId)
        {
            var caller = this.Caller();
            if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
            if (!SeesWholeCondominium(caller) && !string.Equals(caller.UserId, residentId, StringComparison.Ordinal))
                return this.Forbidden();

            var visitors = await _visitorService.GetByResidentAsync(residentId);
            return Ok(Visible(caller, visitors));
        }

        // Portaria recebe os visitantes: vê os do condomínio, como a administração.
        private static bool SeesWholeCondominium(Caller caller) => caller.IsAdministrator || caller.IsDoorman;

        private static List<Visitor> Visible(Caller caller, IEnumerable<Visitor> visitors) =>
            visitors
                .Where(visitor => ResourceAccess.CanSee(caller, visitor.ResidentId, visitor.CondominiumId, SeesWholeCondominium(caller)))
                .ToList();

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] CreateVisitorDto dto)
        {
            var residentId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrEmpty(residentId)) return Unauthorized();

            dto.ResidentId = residentId;

            var visitor = await _visitorService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = visitor.Id }, visitor);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Visitor>> Update(string id, [FromBody] UpdateVisitorDto dto)
        {
            var visitor = await _visitorService.UpdateAsync(id, dto);
            return visitor is null ? NotFound() : Ok(visitor);
        }

        [HttpPost("check-in/{id}")]
        public async Task<ActionResult> CheckIn(string id)
        {
            var existingVisitor = await _visitorService.GetByIdAsync(id);

            if (existingVisitor == null)
                return NotFound();

            await _visitorService.CheckInAsync(id);
            return NoContent();
        }

        [HttpPost("check-out/{id}")]
        public async Task<ActionResult> CheckOut(string id)
        {
            var existingVisitor = await _visitorService.GetByIdAsync(id);

            if (existingVisitor == null)
                return NotFound();

            await _visitorService.CheckOutAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            id = id.Trim();
            _logger.LogInformation("Solicitação para remover visitante {VisitorId}", id);

            if (!ObjectId.TryParse(id, out _))
                return BadRequest(new { message = "Identificador de visitante inválido." });

            try
            {
                var existingVisitor = await _visitorService.GetByIdAsync(id);

                if (existingVisitor == null)
                    return NotFound(new { message = "Visitante não encontrado." });

                await _visitorService.RemoveAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover visitante {VisitorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Não foi possível remover o visitante. Tente novamente." });
            }
        }
    }
}

