using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssemblyController : ControllerBase
{
    private readonly IAssemblyService _service;
    private readonly ILogger<AssemblyController> _logger;

    public AssemblyController(IAssemblyService service, ILogger<AssemblyController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool history = false)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var assemblies = history ? await _service.GetHistoryAsync() : await _service.GetAllAsync();
        return Ok(caller.InCondominium(assemblies, assembly => assembly.CondominiumId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var assembly = await _service.GetByIdAsync(id);
        if (assembly is null) return NotFound();

        return this.DenyOtherCondominium(this.Caller(), assembly.CondominiumId) ?? Ok(assembly);
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetByCondominium(string condominiumId, [FromQuery] bool history = false)
    {
        if (this.DenyOtherCondominium(this.Caller(), condominiumId) is { } denied) return denied;

        var assemblies = history
            ? await _service.GetHistoryByCondominiumAsync(condominiumId)
            : await _service.GetByCondominiumAsync(condominiumId);
        return Ok(assemblies);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssemblyDto dto)
    {
        var caller = this.Caller();
        if (this.DenyManage(caller, dto.CondominiumId, caller.IsAdministrator) is { } denied) return denied;

        _logger.LogInformation(
            "Criando assembleia {Title}. StartDate: {StartDate:o}; EndDate: {EndDate:o}; ScheduledDate: {ScheduledDate:o}",
            dto.Title,
            dto.StartDate,
            dto.EndDate,
            dto.ScheduledDate);

        try
        {
            var createdAssembly = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdAssembly.Id }, createdAssembly);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAssemblyDto dto)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        try
        {
            var updatedAssembly = await _service.UpdateAsync(id, dto);
            if (updatedAssembly is null) return NotFound();

            return Ok(updatedAssembly);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        try
        {
            var deletedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _service.DeleteAsync(id, deletedBy);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover assembleia {AssemblyId}", id);
            return BadRequest(new { message = "Não foi possível remover a assembleia informada." });
        }
    }

    // Escrita da administração só no próprio condomínio; assembleia de outro condomínio responde 404.
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

