using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;
using MongoDB.Bson;

namespace GestaoPro.API.Services;

public class PetAppService : IPetService
{
    private readonly PetRepository _petRepository;
    private readonly UserRepository _userRepository;
    private readonly UnitRepository _unitRepository;

    public PetAppService(
        PetRepository petRepository,
        UserRepository userRepository,
        UnitRepository unitRepository)
    {
        _petRepository = petRepository;
        _userRepository = userRepository;
        _unitRepository = unitRepository;
    }

    public async Task<List<PetResponse>> GetAllByCondominiumAsync(string condominiumId)
    {
        var pets = await _petRepository.GetAllByCondominiumAsync(condominiumId);
        return pets.Select(MapToResponse).ToList();
    }

    public async Task<List<PetResponse>> GetByUnitAsync(string unitId)
    {
        var pets = await _petRepository.GetByUnitAsync(unitId);
        return pets.Select(MapToResponse).ToList();
    }

    public async Task<List<PetResponse>> GetByOwnerAsync(string ownerId)
    {
        var pets = await _petRepository.GetByOwnerAsync(ownerId);
        return pets.Select(MapToResponse).ToList();
    }

    public async Task<PetResponse?> GetByIdAsync(string id)
    {
        var pet = await _petRepository.GetByIdAsync(id);
        return pet is null ? null : MapToResponse(pet);
    }

    public async Task<PetResponse> CreateAsync(CreatePetRequest request)
    {
        // Verifica se o dono existe e estÃ¡ ativo
        var owner = await _userRepository.GetByIdAsync(request.OwnerId);
        if (owner is null || !owner.IsActive)
            throw new InvalidOperationException("Morador nÃ£o encontrado ou inativo.");

        // Verifica se o dono Ã© morador ou sÃ­ndico
        if (owner.Role != UserRole.morador && owner.Role != UserRole.sindico)
            throw new InvalidOperationException("Apenas moradores e sÃ­ndicos podem cadastrar pets.");

        var unitId = await ResolveUnitIdAsync(request.CondominiumId, request.UnitId);
        if (unitId is null)
            throw new InvalidOperationException("Unidade não encontrada.");

        // Verifica se a unidade existe
        var unit = await _unitRepository.GetByIdAsync(unitId);
        if (unit is null)
            throw new InvalidOperationException("Unidade nÃ£o encontrada.");

        // Verifica se o dono pertence Ã  unidade
        if (!unit.ResidentIds.Contains(request.OwnerId))
            throw new InvalidOperationException("O morador nÃ£o pertence a essa unidade.");

        var pet = new Pet
        {
            OwnerId = request.OwnerId,
            UnitId = unitId,
            CondominiumId = request.CondominiumId,
            Name = request.Name,
            Species = request.Species,
            Breed = request.Breed ?? string.Empty,
            Color = request.Color ?? string.Empty,
            Vaccination = request.Vaccination,
            Observations = request.Observations ?? string.Empty
        };

        var created = await _petRepository.CreateAsync(pet);
        return MapToResponse(created);
    }

    public async Task<PetResponse?> UpdateAsync(string id, UpdatePetRequest request)
    {
        var pet = await _petRepository.GetByIdAsync(id);
        if (pet is null) return null;

        if (request.Name is not null) pet.Name = request.Name;
        if (request.Species is not null) pet.Species = request.Species.Value;
        if (request.Breed is not null) pet.Breed = request.Breed;
        if (request.Color is not null) pet.Color = request.Color;
        if (request.Vaccination is not null) pet.Vaccination = request.Vaccination.Value;
        if (request.Observations is not null) pet.Observations = request.Observations;
        if (request.IsActive is not null) pet.IsActive = request.IsActive.Value;

        await _petRepository.UpdateAsync(id, pet);
        return MapToResponse(pet);
    }

    public async Task<bool>  DeleteAsync(string id)
    {
        var pet = await _petRepository.GetByIdAsync(id);
        if (pet is null) return false;
        await _petRepository. DeleteAsync(id);
        return true;
    }

    private static PetResponse MapToResponse(Pet p) => new()
    {
        Id = p.Id!,
        OwnerId = p.OwnerId,
        UnitId = p.UnitId,
        CondominiumId = p.CondominiumId,
        Name = p.Name,
        Species = p.Species,
        Breed = p.Breed,
        Color = p.Color,
        Vaccination = p.Vaccination,
        Observations = p.Observations,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private async Task<string?> ResolveUnitIdAsync(string condominiumId, string unitIdOrLabel)
    {
        if (ObjectId.TryParse(unitIdOrLabel, out _))
            return unitIdOrLabel;

        var unit = await _unitRepository.FindByLabelAsync(condominiumId, unitIdOrLabel);
        return unit?.Id;
    }
}

