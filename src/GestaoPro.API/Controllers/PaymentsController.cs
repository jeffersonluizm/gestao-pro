using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> PostPayment([FromBody] CreatePaymentDto dto)
    {
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
        var payment = await _paymentService.GetByIdAsync(id);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("bill/{billId}")]
    public async Task<IActionResult> GetByBillId(string billId)
    {
        var payments = await _paymentService.GetByBillIdAsync(billId);
        return Ok(payments);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePaymentDto dto)
    {
        var payment = await _paymentService.UpdateAsync(id, dto);
        return payment is null ? NotFound() : Ok(payment);
    }
}

