using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly IBillService _billService;
    private readonly IUserDirectory _users;

    public BillsController(IBillService billService, IUserDirectory users)
    {
        _billService = billService;
        _users = users;
    }

    [HttpPost]
    [DemoQuota("bills")]
    public async Task<IActionResult> Create([FromBody] CreateBillDto dto)
    {
        // Admin/síndico emite cobrança só no próprio condomínio.
        var caller = this.Caller();
        if (this.DenyManage(caller, dto.CondominiumId, caller.IsAdministrator) is { } denied) return denied;
        if (await DenyOtherCondominiumResidentAsync(caller, dto.UserId) is { } otherResident) return otherResident;

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
        var caller = this.Caller();
        var existing = await _billService.GetBillByIdAsync(id);
        if (existing is null) return NotFound();
        if (this.DenyManage(caller, existing.CondominiumId, caller.IsAdministrator) is { } denied) return denied;
        // Não move a cobrança para outro condomínio.
        if (dto.CondominiumId is not null && this.DenyManage(caller, dto.CondominiumId, staff: true) is { } moved) return moved;
        if (dto.UserId is not null && await DenyOtherCondominiumResidentAsync(caller, dto.UserId) is { } otherResident)
            return otherResident;

        var bill = await _billService.UpdateBillAsync(id, dto);
        return bill is null ? NotFound() : Ok(bill);
    }

    // A cobrança é do morador: ele precisa ser do condomínio da cobrança (o do token); outro ou inexistente: 404.
    private async Task<ActionResult?> DenyOtherCondominiumResidentAsync(Caller caller, string userId)
    {
        var resident = await _users.GetByIdAsync(userId);
        return this.DenyManage(caller, resident?.CondominiumId, staff: true);
    }

    // Cobrança não faz parte do fluxo do porteiro; sem condomínio no token, nada é lido.
    private ActionResult? DenyByRole(Caller caller)
    {
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        return caller.IsDoorman ? this.Forbidden() : null;
    }
}

