using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services
{
    public class VisitorAppService
    {
        private readonly IVisitorRepository _repository;

        public VisitorAppService(IVisitorRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Visitor>> GetAllAsync() =>
            await _repository.GetAllAsync();

        public async Task<List<Visitor>> GetByResidentAsync(string residentId) =>
            await _repository.GetByResidentAsync(residentId);

        public async Task<Visitor?> GetByIdAsync(string id) =>
           await _repository.GetByIdAsync(id);

        public async Task<Visitor> CreateAsync(CreateVisitorDto dto)
        {
            var visitor = new Visitor
            {
                FullName = dto.FullName,
                Document = dto.Document,
                Phone = dto.Phone,
                ResidentId = dto.ResidentId,
                CondominiumId = dto.CondominiumId,
                UnitId = dto.UnitId,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow
            };
            return await _repository.CreateAsync(visitor);
        }

        public async Task<Visitor?> UpdateAsync(string id, UpdateVisitorDto dto)
        {
            var visitor = await _repository.GetByIdAsync(id);
            if (visitor is null) return null;

            if (dto.FullName is not null) visitor.FullName = dto.FullName;
            if (dto.Document is not null) visitor.Document = dto.Document;
            if (dto.Phone is not null) visitor.Phone = dto.Phone;
            if (dto.ResidentId is not null) visitor.ResidentId = dto.ResidentId;
            if (dto.CondominiumId is not null) visitor.CondominiumId = dto.CondominiumId;
            if (dto.UnitId is not null) visitor.UnitId = dto.UnitId;
            if (dto.Reason is not null) visitor.Reason = dto.Reason;
            if (dto.Status is not null) visitor.Status = dto.Status;

            await _repository.UpdateAsync(id, visitor);
            return visitor;
        }

        public async Task CheckInAsync(string id) => 
            await _repository.CheckInAsync(id);

        public async Task CheckOutAsync(string id) => 
            await _repository.CheckOutAsync(id);

        public async Task RemoveAsync(string id) =>
            await _repository.DeleteAsync(id);
    }
}

