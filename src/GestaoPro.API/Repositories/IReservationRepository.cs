using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllAsync();
    Task<Reservation?> GetByIdAsync(string id);
    Task<List<Reservation>> GetByCondominiumAsync(string condominiumId);
    Task<List<Reservation>> GetByUserAsync(string userId);
    Task<List<Reservation>> GetByCommonAreaAsync(string commonAreaId);
    Task<List<Reservation>> GetByStatusAsync(string status);
    Task<List<Reservation>> GetByDateAsync(string date);
    Task<Reservation> CreateAsync(Reservation reservation);
    Task UpdateAsync(string id, Reservation reservation);
    Task DeleteAsync(string id);
}

