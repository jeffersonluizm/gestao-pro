using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
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
            var parkings = await _parkingService.GetAllAsync();
            return Ok(parkings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Parking>> GetById(string id)
        {
            var parking = await _parkingService.GetByIdAsync(id);

            if (parking == null)
                return NotFound();

            return Ok(parking);
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<Parking>>> GetAvailable()
        {
            var availableParkings = await _parkingService.GetAvailableAsync();
            return Ok(availableParkings);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateParkingDto dto)
        {
            var parking = await _parkingService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = parking.Id }, parking);
        }

        [HttpPost("reserve/{id}")]
        public async Task<ActionResult> Reserve(string id)
        {
            var userId = Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _parkingService.ReserveAsync(id, userId);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Parking>> Update(string id, UpdateParkingDto dto)
        {
            var parking = await _parkingService.UpdateAsync(id, dto);
            return parking is null ? NotFound() : Ok(parking);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateStatus(string id, [FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("O status Ã© obrigatÃ³rio.");

            await _parkingService.UpdateStatusAsync(id, status);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            await _parkingService.DeleteAsync(id);
            return NoContent();
        }
    }
}

