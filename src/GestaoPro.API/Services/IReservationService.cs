using GestaoPro.API.DTOs;
using GestaoPro.API.Models;

namespace GestaoPro.API.Services;

public interface IReservationService
{
    Task<List<Reservation>> GetAllAsync();
    Task<Reservation?> GetByIdAsync(string id);
    Task<List<Reservation>> GetByCondominiumAsync(string condominiumId);
    Task<List<Reservation>> GetByUserAsync(string userId);
    Task<List<Reservation>> GetByCommonAreaAsync(string commonAreaId);
    Task<List<Reservation>> GetByStatusAsync(string status);
    Task<List<Reservation>> GetByDateAsync(string date);
    Task<Reservation> CreateAsync(CreateReservationDto dto);
    Task<Reservation?> UpdateAsync(string id, UpdateReservationDto dto);
    Task<bool> DeleteAsync(string id);
}

