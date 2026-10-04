using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services
{
    public class ParkingAppService
    {
        private readonly IParkingRepository _repository;

        public ParkingAppService(IParkingRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Parking>> GetAllAsync() => _repository.GetAllAsync();

        public Task<Parking?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

        public Task<List<Parking>> GetAvailableAsync() => _repository.GetAvailableAsync();

        public async Task<Parking> CreateAsync(CreateParkingDto dto)
        {
            var number = dto.Number.Trim();
            await EnsureUniqueNumberAsync(number, dto.CondominiumId, exceptId: null);

            var parking = new Parking
            {
                Number = number,
                Type = dto.Type,
                CondominiumId = dto.CondominiumId,
                IsShared = dto.IsShared,
                UnitId = dto.UnitId,
                CreatedAt = DateTime.UtcNow
            };
            return await _repository.CreateAsync(parking);
        }

        public Task ReserveAsync(string id, string userId) => _repository.ReserveAsync(id, userId);

        public async Task<Parking?> UpdateAsync(string id, UpdateParkingDto dto)
        {
            var parking = await _repository.GetByIdAsync(id);
            if (parking is null) return null;

            if (dto.Number is not null || dto.CondominiumId is not null)
            {
                var number = dto.Number?.Trim() ?? parking.Number;
                await EnsureUniqueNumberAsync(number, dto.CondominiumId ?? parking.CondominiumId, exceptId: id);
                parking.Number = number;
            }
            if (dto.Type is not null) parking.Type = dto.Type;
            if (dto.CondominiumId is not null) parking.CondominiumId = dto.CondominiumId;
            if (dto.Status is not null) parking.Status = dto.Status;
            if (dto.IsShared is not null) parking.IsShared = dto.IsShared.Value;
            if (dto.ResidentId is not null) parking.ResidentId = dto.ResidentId;
            if (dto.UnitId is not null) parking.UnitId = dto.UnitId;
            if (dto.ReservedByUserId is not null) parking.ReservedByUserId = dto.ReservedByUserId;

            if (parking.Status == "available")
            {
                parking.ReservedByUserId = null;
                parking.ReservedAt = null;
            }

            await _repository.UpdateAsync(id, parking);
            return parking;
        }

        public Task UpdateStatusAsync(string id, string status) => _repository.UpdateStatusAsync(id, status);

        public Task DeleteAsync(string id) => _repository.DeleteAsync(id);

        // O número da vaga é único dentro do condomínio (outro condomínio pode repetir). Repetido: o controller devolve 409.
        private async Task EnsureUniqueNumberAsync(string number, string? condominiumId, string? exceptId)
        {
            var existing = await _repository.GetAllAsync();
            if (existing.Any(item =>
                item.Id != exceptId &&
                string.Equals(item.CondominiumId ?? string.Empty, condominiumId ?? string.Empty, StringComparison.Ordinal) &&
                string.Equals(item.Number.Trim(), number, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Já existe uma vaga cadastrada com esse número.");
            }
        }
    }
}

