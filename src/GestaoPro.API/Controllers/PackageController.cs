using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Controllers;

// Pacote é do destinatário: o morador vê os próprios; portaria e administração, os do condomínio.
// Escritas (só administração, pelo middleware) apenas no próprio condomínio.
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
    public async Task<IActionResult> GetAll()
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        return Ok(Visible(caller, await _service.GetAllAsync()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var package = await _service.GetByIdAsync(id);
        if (package == null) return NotFound();

        return this.Deny(ResourceAccess.Decide(caller, package.RecipientId, package.CondominiumId, IsStaff(caller)))
            ?? Ok(package);
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        var caller = this.Caller();
        if (DenyCondominium(caller, condominiumId) is { } denied) return denied;

        return Ok(Visible(caller, await _service.GetByCondominiumAsync(condominiumId)));
    }

    [HttpGet("condominium/{condominiumId}/pending")]
    public async Task<IActionResult> GetPending(string condominiumId)
    {
        var caller = this.Caller();
        if (DenyCondominium(caller, condominiumId) is { } denied) return denied;

        return Ok(Visible(caller, await _service.GetPendingByCondominiumAsync(condominiumId)));
    }

    [HttpPost]
    [DemoQuota("packages")]
    public async Task<IActionResult> Register([FromBody] CreatePackageDto dto)
    {
        // Sem o cabeçalho X-Condominium-Id, vale o condomínio do token.
        var caller = this.Caller();
        var condominiumId = Request.Headers["X-Condominium-Id"].ToString();
        if (string.IsNullOrWhiteSpace(condominiumId)) condominiumId = caller.CondominiumId ?? string.Empty;
        if (this.DenyManage(caller, condominiumId, caller.IsAdministrator) is { } denied) return denied;

        dto.CondominiumId = condominiumId;

        var package = await _service.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = package.Id }, package);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePackageDto dto)
    {
        var (_, denied) = await FindManagedAsync(id);
        if (denied is not null) return denied;
        // Não move o pacote para outro condomínio.
        if (dto.CondominiumId is not null && this.DenyManage(this.Caller(), dto.CondominiumId, staff: true) is { } moved)
            return moved;

        var package = await _service.UpdateAsync(id, dto);
        return package is null ? NotFound() : Ok(package);
    }

    [HttpPatch("{id}/pickup")]
    public async Task<IActionResult> PickUp(string id)
    {
        var (existing, denied) = await FindManagedAsync(id);
        if (denied is not null) return denied;
        if (existing!.Status == "picked_up")
            return BadRequest(new { message = "Package already picked up." });

        await _service.PickUpAsync(id);
        return Ok(new { message = "Package picked up successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var (_, denied) = await FindManagedAsync(id);
        if (denied is not null) return denied;

        await _service.DeleteAsync(id);
        return NoContent();
    }

    // Portaria recebe os pacotes: vê os do condomínio, como a administração.
    private static bool IsStaff(Caller caller) => caller.IsAdministrator || caller.IsDoorman;

    private static List<Package> Visible(Caller caller, IEnumerable<Package> packages) =>
        packages
            .Where(package => ResourceAccess.CanSee(caller, package.RecipientId, package.CondominiumId, IsStaff(caller)))
            .ToList();

    private ActionResult? DenyCondominium(Caller caller, string condominiumId)
    {
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        return string.Equals(caller.CondominiumId, condominiumId, StringComparison.Ordinal) ? null : NotFound();
    }

    private async Task<(Package? Package, ActionResult? Denied)> FindManagedAsync(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return (null, this.Deny(AccessDecision.InvalidSession));
        if (!caller.IsAdministrator) return (null, this.Forbidden());

        var package = await _service.GetByIdAsync(id);
        if (package == null) return (null, NotFound());
        return (package, this.DenyManage(caller, package.CondominiumId, staff: true));
    }
}
