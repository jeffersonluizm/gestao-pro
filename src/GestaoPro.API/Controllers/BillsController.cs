using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly IBillService _billService;

    public BillsController(IBillService billService)
    {
        _billService = billService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBillDto dto)
    {
        var result = await _billService.CreateBillAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (DenyByRole(caller) is { } denied) return denied;

        var bill = await _billService.GetBillByIdAsync(id);
        if (bill is null) return NotFound();

        return this.Deny(ResourceAccess.Decide(caller, bill.UserId, bill.CondominiumId, caller.IsAdministrator))
            ?? Ok(bill);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var caller = this.Caller();
        if (DenyByRole(caller) is { } denied) return denied;
        if (!caller.IsAdministrator && !string.Equals(caller.UserId, userId, StringComparison.Ordinal))
            return this.Forbidden();

        var bills = await _billService.GetUserBillsAsync(userId);
        return Ok(bills
            .Where(bill => ResourceAccess.CanSee(caller, bill.UserId, bill.CondominiumId, caller.IsAdministrator))
            .ToList());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBillDto dto)
    {
        var bill = await _billService.UpdateBillAsync(id, dto);
        return bill is null ? NotFound() : Ok(bill);
    }

    // Cobrança não faz parte do fluxo do porteiro; sem condomínio no token, nada é lido.
    private ActionResult? DenyByRole(Caller caller)
    {
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        return caller.IsDoorman ? this.Forbidden() : null;
    }
}

