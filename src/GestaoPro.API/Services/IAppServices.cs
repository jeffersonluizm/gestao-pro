using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

// Contratos dos serviços usados pelos controllers de ocorrências, pets, unidades, condomínios e encomendas.
// Existem para que o controle de acesso desses controllers possa ser testado com Moq, sem MongoDB.

public interface IOccurrenceService
{
    Task<List<OccurrenceResponse>> GetAllByCondominiumAsync(string condominiumId);
    Task<List<OccurrenceResponse>> GetByCondominiumAndStatusAsync(string condominiumId, OccurrenceStatus status);
    Task<List<OccurrenceResponse>> GetByUserAsync(string userId);
    Task<OccurrenceResponse?> GetByIdAsync(string id);
    Task<OccurrenceResponse> CreateAsync(CreateOccurrenceRequest request);
    Task<OccurrenceResponse?> UpdateAsync(string id, UpdateOccurrenceRequest request);
    Task<OccurrenceResponse?> UpdateStatusAsync(string id, UpdateOccurrenceStatusRequest request);
    Task<bool> DeleteAsync(string id);
}

public interface IPetService
{
    Task<List<PetResponse>> GetAllByCondominiumAsync(string condominiumId);
    Task<List<PetResponse>> GetByUnitAsync(string unitId);
    Task<List<PetResponse>> GetByOwnerAsync(string ownerId);
    Task<PetResponse?> GetByIdAsync(string id);
    Task<PetResponse> CreateAsync(CreatePetRequest request);
    Task<PetResponse?> UpdateAsync(string id, UpdatePetRequest request);
    Task<bool> DeleteAsync(string id);
}

public interface IUnitService
{
    Task<List<UnitResponse>> GetAllByCondominiumAsync(string condominiumId);
    Task<UnitResponse?> GetByIdAsync(string id);
    Task<UnitResponse> CreateAsync(CreateUnitRequest request);
    Task<UnitResponse?> UpdateAsync(string id, UpdateUnitRequest request);
    Task<bool> AddResidentAsync(string unitId, string residentId);
    Task<bool> RemoveResidentAsync(string unitId, string residentId);
    Task<bool> DeleteAsync(string id);
}

public interface ICondominiumService
{
    Task<List<CondominiumSummary>> GetPublicAsync();
    Task<CondominiumResponse?> GetByIdAsync(string id);
    Task<CondominiumResponse> CreateAsync(CreateCondominiumRequest request);
    Task<CondominiumResponse?> UpdateAsync(string id, UpdateCondominiumRequest request);
    Task<bool> DeleteAsync(string id);
}

public interface IDeliveryService
{
    Task<List<Delivery>> GetAsync();
    Task<Delivery?> GetByIdAsync(string id);
    Task<List<Delivery>> GetByResidentAsync(string residentId);
    Task CreateAsync(Delivery delivery);
    Task MarkAsPickedUpAsync(string id, string pickedUpBy);
    Task<Delivery?> UpdateAsync(string id, UpdateDeliveryDto dto);
    Task RemoveAsync(string id);
}
