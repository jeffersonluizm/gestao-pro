using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackageController : ControllerBase
{
    private readonly PackageAppService _service;

    public PackageController(PackageAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var package = await _service.GetByIdAsync(id);
        return package == null ? NotFound() : Ok(package);
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetByCondominium(string condominiumId) =>
        Ok(await _service.GetByCondominiumAsync(condominiumId));

    [HttpGet("condominium/{condominiumId}/pending")]
    public async Task<IActionResult> GetPending(string condominiumId) =>
        Ok(await _service.GetPendingByCondominiumAsync(condominiumId));

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] CreatePackageDto dto)
    {
        var condominiumId = Request.Headers["X-Condominium-Id"].ToString();
        if (string.IsNullOrEmpty(condominiumId)) return Unauthorized();

        dto.CondominiumId = condominiumId;

        var package = await _service.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = package.Id }, package);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePackageDto dto)
    {
        var package = await _service.UpdateAsync(id, dto);
        return package is null ? NotFound() : Ok(package);
    }

    [HttpPatch("{id}/pickup")]
    public async Task<IActionResult> PickUp(string id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.Status == "picked_up")
            return BadRequest(new { message = "Package already picked up." });

        await _service.PickUpAsync(id);
        return Ok(new { message = "Package picked up successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _service.DeleteAsync(id);
        return NoContent();
    }
}

