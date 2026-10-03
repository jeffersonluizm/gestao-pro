using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class PackageAppService
{
    private readonly IPackageRepository _repository;

    public PackageAppService(IPackageRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Package>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Package?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<Package>> GetByCondominiumAsync(string condominiumId) =>
        _repository.GetByCondominiumAsync(condominiumId);

    public Task<List<Package>> GetPendingByCondominiumAsync(string condominiumId) =>
        _repository.GetPendingByCondominiumAsync(condominiumId);

    public async Task<Package> RegisterAsync(CreatePackageDto dto)
    {
        var package = new Package
        {
            TrackingCode = dto.TrackingCode,
            Carrier = dto.Carrier,
            RecipientId = dto.RecipientId,
            RecipientName = dto.RecipientName,
            UnitNumber = dto.UnitNumber,
            CondominiumId = dto.CondominiumId,
            Observations = dto.Observations,
            Status = "pending",
            ReceivedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        return await _repository.CreateAsync(package);
    }

    public Task PickUpAsync(string id) => _repository.PickUpAsync(id);

    public async Task<Package?> UpdateAsync(string id, UpdatePackageDto dto)
    {
        var package = await _repository.GetByIdAsync(id);
        if (package is null) return null;

        if (dto.TrackingCode is not null) package.TrackingCode = dto.TrackingCode;
        if (dto.Carrier is not null) package.Carrier = dto.Carrier;
        if (dto.RecipientId is not null) package.RecipientId = dto.RecipientId;
        if (dto.RecipientName is not null) package.RecipientName = dto.RecipientName;
        if (dto.UnitNumber is not null) package.UnitNumber = dto.UnitNumber;
        if (dto.CondominiumId is not null) package.CondominiumId = dto.CondominiumId;
        if (dto.Status is not null) package.Status = dto.Status;
        if (dto.Observations is not null) package.Observations = dto.Observations;

        await _repository.UpdateAsync(id, package);
        return package;
    }

    public Task DeleteAsync(string id) => _repository.DeleteAsync(id);
}

