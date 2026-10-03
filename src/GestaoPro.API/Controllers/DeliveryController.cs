using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly DeliveryService _deliveryService;

    public DeliveryController(DeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Delivery>>> GetAll() =>
        Ok(await _deliveryService.GetAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Delivery>> GetById(string id)
    {
        var delivery = await _deliveryService.GetByIdAsync(id);
        return delivery is null ? NotFound() : Ok(delivery);
    }

    [HttpGet("resident/{residentId}")]
    public async Task<ActionResult<List<Delivery>>> GetByResident(string residentId) =>
        Ok(await _deliveryService.GetByResidentAsync(residentId));

    [HttpPost]
    public async Task<ActionResult<Delivery>> Create(Delivery delivery)
    {
        try
        {
            await _deliveryService.CreateAsync(delivery);
            return CreatedAtAction(nameof(GetById), new { id = delivery.Id }, delivery);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/pickup")]
    public async Task<ActionResult> MarkAsPickedUp(string id, [FromQuery] string pickedUpBy)
    {
        if (string.IsNullOrWhiteSpace(pickedUpBy)) return BadRequest("Nome obrigatorio.");

        try
        {
            await _deliveryService.MarkAsPickedUpAsync(id, pickedUpBy);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Delivery>> Update(string id, [FromBody] UpdateDeliveryDto dto)
    {
        var delivery = await _deliveryService.UpdateAsync(id, dto);
        return delivery is null ? NotFound() : Ok(delivery);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _deliveryService.RemoveAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/qrcode")]
    public async Task<ActionResult> GenerateQRCode(string id)
    {
        var delivery = await _deliveryService.GetByIdAsync(id);
        if (delivery is null) return NotFound();

        var text = $"Encomenda: {delivery.TrackingCode} | Morador: {delivery.ResidentId}";
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        var base64 = Convert.ToBase64String(qrCodeBytes);
        return Ok(new { qrCodeBase64 = $"data:image/png;base64,{base64}" });
    }
}
