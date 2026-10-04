using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace GestaoPro.API.Controllers;

// Encomenda é do morador: ele vê e retira as próprias; portaria e administração cuidam das do condomínio.
// Encomendas antigas não guardam o condomínio: ele vem do cadastro do morador (IUserDirectory).
[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;
    private readonly IUserDirectory _users;

    public DeliveryController(IDeliveryService deliveryService, IUserDirectory users)
    {
        _deliveryService = deliveryService;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<List<Delivery>>> GetAll()
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var deliveries = IsStaff(caller)
            ? await _deliveryService.GetAsync()
            : await _deliveryService.GetByResidentAsync(caller.UserId!);
        return Ok(await VisibleAsync(caller, deliveries));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Delivery>> GetById(string id)
    {
        var (delivery, denied) = await FindVisibleAsync(id);
        return denied ?? Ok(delivery);
    }

    [HttpGet("resident/{residentId}")]
    public async Task<ActionResult<List<Delivery>>> GetByResident(string residentId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!IsStaff(caller) && !string.Equals(caller.UserId, residentId, StringComparison.Ordinal))
            return this.Forbidden();

        return Ok(await VisibleAsync(caller, await _deliveryService.GetByResidentAsync(residentId)));
    }

    [HttpPost]
    public async Task<ActionResult<Delivery>> Create(Delivery delivery)
    {
        // Portaria e administração cadastram só para morador do próprio condomínio; o condomínio é o do token.
        var caller = this.Caller();
        var resident = await _users.GetByIdAsync(delivery.ResidentId);
        if (this.DenyManage(caller, resident?.CondominiumId, IsStaff(caller)) is { } denied) return denied;

        delivery.CondominiumId = caller.CondominiumId;

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

        // O morador retira as próprias; a portaria registra a retirada das do condomínio.
        var (_, denied) = await FindVisibleAsync(id);
        if (denied is not null) return denied;

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
        if (await DenyManageAsync(id) is { } denied) return denied;
        if (dto.ResidentId is not null)
        {
            // Não troca para morador de outro condomínio.
            var resident = await _users.GetByIdAsync(dto.ResidentId);
            if (this.DenyManage(this.Caller(), resident?.CondominiumId, staff: true) is { } moved) return moved;
        }

        var delivery = await _deliveryService.UpdateAsync(id, dto);
        return delivery is null ? NotFound() : Ok(delivery);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (await DenyManageAsync(id) is { } denied) return denied;

        await _deliveryService.RemoveAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/qrcode")]
    public async Task<ActionResult> GenerateQRCode(string id)
    {
        var (delivery, denied) = await FindVisibleAsync(id);
        if (denied is not null) return denied;

        var text = $"Encomenda: {delivery!.TrackingCode} | Morador: {delivery.ResidentId}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        var base64 = Convert.ToBase64String(qrCodeBytes);

        return Ok(new { qrCodeBase64 = $"data:image/png;base64,{base64}" });
    }

    // Portaria recebe as encomendas: vê as do condomínio, como a administração.
    private static bool IsStaff(Caller caller) => caller.IsAdministrator || caller.IsDoorman;

    private async Task<(Delivery? Delivery, ActionResult? Denied)> FindVisibleAsync(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return (null, this.Deny(AccessDecision.InvalidSession));

        var delivery = await _deliveryService.GetByIdAsync(id);
        if (delivery is null) return (null, NotFound());

        var condominiumId = await CondominiumOfAsync(delivery, new Dictionary<string, string?>());
        return (delivery, this.Deny(ResourceAccess.Decide(caller, delivery.ResidentId, condominiumId, IsStaff(caller))));
    }

    private async Task<ActionResult?> DenyManageAsync(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession);
        if (!IsStaff(caller)) return this.Forbidden();

        var delivery = await _deliveryService.GetByIdAsync(id);
        if (delivery is null) return NotFound();
        return this.DenyManage(caller, await CondominiumOfAsync(delivery, new Dictionary<string, string?>()), staff: true);
    }

    private async Task<List<Delivery>> VisibleAsync(Caller caller, IEnumerable<Delivery> deliveries)
    {
        var residentCondominiums = new Dictionary<string, string?>();
        var visible = new List<Delivery>();
        foreach (var delivery in deliveries)
        {
            var condominiumId = await CondominiumOfAsync(delivery, residentCondominiums);
            if (ResourceAccess.CanSee(caller, delivery.ResidentId, condominiumId, IsStaff(caller)))
                visible.Add(delivery);
        }
        return visible;
    }

    // Condomínio gravado na encomenda ou, nas antigas, o do cadastro do morador (consultado uma vez por morador).
    private async Task<string?> CondominiumOfAsync(Delivery delivery, Dictionary<string, string?> residentCondominiums)
    {
        if (!string.IsNullOrWhiteSpace(delivery.CondominiumId)) return delivery.CondominiumId;
        if (string.IsNullOrWhiteSpace(delivery.ResidentId)) return null;

        if (!residentCondominiums.TryGetValue(delivery.ResidentId, out var condominiumId))
        {
            condominiumId = (await _users.GetByIdAsync(delivery.ResidentId))?.CondominiumId;
            residentCondominiums[delivery.ResidentId] = condominiumId;
        }
        return condominiumId;
    }
}
