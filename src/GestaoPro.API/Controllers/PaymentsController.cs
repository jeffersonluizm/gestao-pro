using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IBillService _billService;

    public PaymentsController(IPaymentService paymentService, IBillService billService)
    {
        _paymentService = paymentService;
        _billService = billService;
    }

    [HttpPost]
    public async Task<IActionResult> PostPayment([FromBody] CreatePaymentDto dto)
    {
        // Só registra pagamento de cobrança do próprio condomínio.
        if (await DenyManageBillAsync(dto.BillId) is { } denied) return denied;

        try 
        {
            var result = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao processar pagamento", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (DenyByRole(caller) is { } denied) return denied;

        var payment = await _paymentService.GetByIdAsync(id);
        if (payment is null) return NotFound();

        return await DenyByBillAsync(caller, payment.BillId) ?? Ok(payment);
    }

    [HttpGet("bill/{billId}")]
    public async Task<IActionResult> GetByBillId(string billId)
    {
        var caller = this.Caller();
        if (DenyByRole(caller) is { } denied) return denied;
        if (await DenyByBillAsync(caller, billId) is { } deniedByBill) return deniedByBill;

        var payments = await _paymentService.GetByBillIdAsync(billId);
        return Ok(payments);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePaymentDto dto)
    {
        var existing = await _paymentService.GetByIdAsync(id);
        if (existing is null) return NotFound();
        if (await DenyManageBillAsync(existing.BillId) is { } denied) return denied;
        // Não move o pagamento para cobrança de outro condomínio.
        if (dto.BillId is not null && await DenyManageBillAsync(dto.BillId) is { } moved) return moved;

        var payment = await _paymentService.UpdateAsync(id, dto);
        return payment is null ? NotFound() : Ok(payment);
    }

    // Escrita: só admin/síndico e só em cobrança do próprio condomínio.
    private async Task<ActionResult?> DenyManageBillAsync(string billId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        if (!caller.IsAdministrator) return this.Forbidden();

        var bill = await _billService.GetBillByIdAsync(billId);
        return this.DenyManage(caller, bill?.CondominiumId, staff: true);
    }

    // Pagamento não guarda dono nem condomínio: vale a regra da cobrança. Sem cobrança, 404.
    private async Task<ActionResult?> DenyByBillAsync(Caller caller, string billId)
    {
        var bill = await _billService.GetBillByIdAsync(billId);
        if (bill is null) return NotFound();
        return this.Deny(ResourceAccess.Decide(caller, bill.UserId, bill.CondominiumId, caller.IsAdministrator));
    }

    // Pagamento não faz parte do fluxo do porteiro; sem condomínio no token, nada é lido.
    private ActionResult? DenyByRole(Caller caller)
    {
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        return caller.IsDoorman ? this.Forbidden() : null;
    }
}

