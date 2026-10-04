using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GestaoPro.API.Services;

public class UnitAppService : IUnitService
{
    private readonly UnitRepository _unitRepository;
    private readonly UserRepository _userRepository;

    public UnitAppService(UnitRepository unitRepository, UserRepository userRepository)
    {
        _unitRepository = unitRepository;
        _userRepository = userRepository;
    }

    // Busca todas as unidades de um condomínio específico e converte para UnitResponse.
    public async Task<List<UnitResponse>> GetAllByCondominiumAsync(string condominiumId)
    {
        var units = await _unitRepository.GetAllByCondominiumAsync(condominiumId);
        return units.Select(MapToResponse).ToList();
    }

    // Busca uma unidade pelo ID.
    public async Task<UnitResponse?> GetByIdAsync(string id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        return unit is null ? null : MapToResponse(unit);
    }

    // Cria unidade.
    public async Task<UnitResponse> CreateAsync(CreateUnitRequest request)
    {
        // Verifica se já existe unidade com mesmo número e bloco no condomínio
        var existing = await _unitRepository.GetByNumberAndBlockAsync(
            request.CondominiumId, request.Number, request.Block);
        if (existing is not null)
            throw new InvalidOperationException(
                "Já existe uma unidade com esse número e bloco nesse condomínio.");

        var unit = new Unit
        {
            CondominiumId = request.CondominiumId,
            Number = request.Number,
            Block = request.Block,
            Floor = request.Floor,
            Type = request.Type
        };

        var created = await _unitRepository.CreateAsync(unit);
        return MapToResponse(created);
    }

    // Atualiza apenas os campos que foram passados, mantém o resto como estava.
    public async Task<UnitResponse?> UpdateAsync(string id, UpdateUnitRequest request)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        if (unit is null) return null;

        if (request.Number is not null) unit.Number = request.Number;
        if (request.Block is not null) unit.Block = request.Block;
        if (request.Floor is not null) unit.Floor = request.Floor;
        if (request.Type is not null) unit.Type = request.Type.Value;

        await _unitRepository.UpdateAsync(id, unit);
        return MapToResponse(unit);
    }

    // Adiciona morador na unidade.
    public async Task<bool> AddResidentAsync(string unitId, string residentId)
    {
        var unit = await _unitRepository.GetByIdAsync(unitId);
        if (unit is null) return false;

        var resident = await _userRepository.GetByIdAsync(residentId);
        if (resident is null) return false;

        // Verifica CPF duplicado na unidade
        foreach (var existingResidentId in unit.ResidentIds)
        {
            var existingResident = await _userRepository.GetByIdAsync(existingResidentId);
            if (existingResident is not null && existingResident.Cpf == resident.Cpf)
                throw new InvalidOperationException(
                    "Já existe um morador com esse CPF nessa unidade.");
        }

        await _unitRepository.AddResidentAsync(unitId, residentId);
        return true;
    }

    // Remove morador da unidade.
    public async Task<bool> RemoveResidentAsync(string unitId, string residentId)
    {
        var unit = await _unitRepository.GetByIdAsync(unitId);
        if (unit is null) return false;

        if (!unit.ResidentIds.Contains(residentId))
            throw new InvalidOperationException(
                "Esse morador não está cadastrado nessa unidade.");

        await _unitRepository.RemoveResidentAsync(unitId, residentId);
        return true;
    }

    // Verifica se a unidade existe antes de deletar.
    public async Task<bool> DeleteAsync(string id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        if (unit is null) return false;

        await _unitRepository.DeleteAsync(id);
        return true;
    }

    // Converte o objeto Unit do banco em UnitResponse com contagem de moradores.
    private static UnitResponse MapToResponse(Unit u) => new(
        u.Id!,
        u.CondominiumId,
        u.Number,
        u.Block,
        u.Floor,
        u.Type,
        u.ResidentIds,
        u.ResidentIds.Count,
        u.LastResidentUpdateAt,
        u.LastResidentUpdateType,
        u.CreatedAt
    );
}

