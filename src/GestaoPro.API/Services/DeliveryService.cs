using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GestaoPro.API.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IMongoCollection<Delivery> _deliveries;
    private readonly CacheService _cache;
    private readonly IHubContext<NotificacaoHub> _hubContext;
    private readonly EmailService _email;
    private readonly SmsService _sms;

    public DeliveryService(
        IOptions<MongoDBSettings> settings,
        CacheService cache,
        IHubContext<NotificacaoHub> hubContext,
        EmailService email,
        SmsService sms)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _deliveries = database.GetCollection<Delivery>("deliveries");
        _cache = cache;
        _hubContext = hubContext;
        _email = email;
        _sms = sms;
    }

    public async Task<List<Delivery>> GetAsync()
    {
        const string cacheKey = "todas_encomendas";
        var cached = _cache.Obter<List<Delivery>>(cacheKey);
        if (cached is not null) return cached;

        var deliveries = await _deliveries.Find(_ => true).ToListAsync();
        _cache.Definir(cacheKey, deliveries, 30);
        return deliveries;
    }

    public async Task<Delivery?> GetByIdAsync(string id) =>
        await _deliveries.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<Delivery>> GetByResidentAsync(string residentId) =>
        await _deliveries.Find(x => x.ResidentId == residentId).ToListAsync();

    public async Task CreateAsync(Delivery delivery)
    {
        var exists = await _deliveries
            .Find(x => x.TrackingCode == delivery.TrackingCode && x.Status == "pending")
            .AnyAsync();
        if (exists)
            throw new InvalidOperationException("Ja existe encomenda ativa com este codigo.");

        await _deliveries.InsertOneAsync(delivery);
        _cache.Remover("todas_encomendas");

        await _hubContext.Clients.Group(delivery.ResidentId)
            .SendAsync("NovaNotificacao", $"Nova encomenda: {delivery.TrackingCode}");
        await _email.EnviarAsync($"morador_{delivery.ResidentId}@condominio.com", "Encomenda chegou", $"Seu codigo: {delivery.TrackingCode}");
        await _sms.EnviarAsync("999999999", $"Encomenda {delivery.TrackingCode} disponivel.");
    }

    public async Task MarkAsPickedUpAsync(string id, string pickedUpBy)
    {
        var delivery = await GetByIdAsync(id);
        if (delivery is null) throw new ArgumentException("Encomenda nao encontrada.");

        var update = Builders<Delivery>.Update
            .Set(x => x.Status, "picked_up")
            .Set(x => x.PickedUpAt, DateTime.UtcNow)
            .Set(x => x.PickedUpBy, pickedUpBy);
        await _deliveries.UpdateOneAsync(x => x.Id == id, update);
        _cache.Remover("todas_encomendas");
    }

    public async Task<Delivery?> UpdateAsync(string id, UpdateDeliveryDto dto)
    {
        var delivery = await GetByIdAsync(id);
        if (delivery is null) return null;

        if (dto.TrackingCode is not null) delivery.TrackingCode = dto.TrackingCode;
        if (dto.Description is not null) delivery.Description = dto.Description;
        if (dto.Type is not null) delivery.Type = dto.Type;
        if (dto.ResidentId is not null) delivery.ResidentId = dto.ResidentId;
        if (dto.UnitId is not null) delivery.UnitId = dto.UnitId;
        if (dto.Status is not null) delivery.Status = dto.Status;
        if (dto.PickedUpBy is not null) delivery.PickedUpBy = dto.PickedUpBy;
        if (dto.Observations is not null) delivery.Observations = dto.Observations;
        if (dto.QrCodeBase64 is not null) delivery.QrCodeBase64 = dto.QrCodeBase64;
        if (dto.FotoUrl is not null) delivery.FotoUrl = dto.FotoUrl;

        var update = Builders<Delivery>.Update
            .Set(x => x.TrackingCode, delivery.TrackingCode)
            .Set(x => x.Description, delivery.Description)
            .Set(x => x.Type, delivery.Type)
            .Set(x => x.ResidentId, delivery.ResidentId)
            .Set(x => x.UnitId, delivery.UnitId)
            .Set(x => x.Status, delivery.Status)
            .Set(x => x.PickedUpAt, delivery.PickedUpAt)
            .Set(x => x.PickedUpBy, delivery.PickedUpBy)
            .Set(x => x.Observations, delivery.Observations)
            .Set(x => x.QrCodeBase64, delivery.QrCodeBase64)
            .Set(x => x.FotoUrl, delivery.FotoUrl);

        await _deliveries.UpdateOneAsync(x => x.Id == id, update);
        _cache.Remover("todas_encomendas");
        return delivery;
    }

    public async Task RemoveAsync(string id)
    {
        await _deliveries.DeleteOneAsync(x => x.Id == id);
        _cache.Remover("todas_encomendas");
    }
}
