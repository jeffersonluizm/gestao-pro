using System.Globalization;
using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _repository;
    private readonly ICommonAreaRepository _commonAreaRepository;
    private readonly IReservationSlotLock _slotLock;
    private readonly TimeProvider _clock;

    public ReservationService(
        IReservationRepository repository,
        ICommonAreaRepository commonAreaRepository,
        IReservationSlotLock? slotLock = null,
        TimeProvider? clock = null)
    {
        _repository = repository;
        _commonAreaRepository = commonAreaRepository;
        _slotLock = slotLock ?? new InProcessReservationSlotLock();
        _clock = clock ?? TimeProvider.System;
    }

    public Task<List<Reservation>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Reservation?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

    public Task<List<Reservation>> GetByCondominiumAsync(string condominiumId) =>
        _repository.GetByCondominiumAsync(condominiumId);

    public Task<List<Reservation>> GetByUserAsync(string userId) =>
        _repository.GetByUserAsync(userId);

    public Task<List<Reservation>> GetByCommonAreaAsync(string commonAreaId) =>
        _repository.GetByCommonAreaAsync(commonAreaId);

    public Task<List<Reservation>> GetByStatusAsync(string status) =>
        _repository.GetByStatusAsync(status);

    public Task<List<Reservation>> GetByDateAsync(string date) =>
        _repository.GetByDateAsync(date);

    public async Task<Reservation> CreateAsync(CreateReservationDto dto)
    {
        // Formato único antes de qualquer conferência: "2026/10/20" e "9:00" viram "2026-10-20" e "09:00".
        dto.Date = NormalizeDate(dto.Date);
        dto.StartTime = NormalizeTime(dto.StartTime);
        dto.EndTime = NormalizeTime(dto.EndTime);

        var commonArea = await _commonAreaRepository.GetByIdAsync(dto.CommonAreaId);
        if (commonArea is null || !commonArea.IsActive)
            throw new InvalidOperationException("Área comum inválida.");

        EnsureDateIsNotPast(dto.Date);
        EnsureReservationTimeIsValid(dto.StartTime, dto.EndTime, commonArea);

        // Conferência de conflito e gravação sob a mesma trava (área e data): sem corrida entre pedidos simultâneos.
        await using var slot = await _slotLock.AcquireAsync(dto.CommonAreaId, dto.Date);
        await EnsureNoConflictAsync(dto.CommonAreaId, dto.Date, dto.StartTime, dto.EndTime);

        var reservation = new Reservation
        {
            CommonAreaId = dto.CommonAreaId,
            UserId = dto.UserId,
            CondominiumId = string.IsNullOrWhiteSpace(dto.CondominiumId) ? commonArea.CondominiumId : dto.CondominiumId,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return await _repository.CreateAsync(reservation);
    }

    public async Task<Reservation?> UpdateAsync(string id, UpdateReservationDto dto)
    {
        var reservation = await _repository.GetByIdAsync(id);
        if (reservation is null) return null;

        // Mudança só de status (aprovar, cancelar) não confere data nem conflito de novo.
        var scheduleChanged = dto.Date is not null || dto.StartTime is not null || dto.EndTime is not null;
        if (dto.Date is not null) reservation.Date = NormalizeDate(dto.Date);
        if (dto.StartTime is not null) reservation.StartTime = NormalizeTime(dto.StartTime);
        if (dto.EndTime is not null) reservation.EndTime = NormalizeTime(dto.EndTime);
        if (dto.Status is not null) reservation.Status = dto.Status;
        if (dto.Observations is not null) reservation.Observations = dto.Observations;

        reservation.UpdatedAt = DateTime.UtcNow;
        if (!scheduleChanged)
        {
            await _repository.UpdateAsync(id, reservation);
            return reservation;
        }

        var commonArea = await _commonAreaRepository.GetByIdAsync(reservation.CommonAreaId);
        if (commonArea is null || !commonArea.IsActive)
            throw new InvalidOperationException("Área comum inválida.");

        EnsureDateIsNotPast(reservation.Date);
        EnsureReservationTimeIsValid(reservation.StartTime, reservation.EndTime, commonArea);

        await using var slot = await _slotLock.AcquireAsync(reservation.CommonAreaId, reservation.Date);
        await EnsureNoConflictAsync(
            reservation.CommonAreaId,
            reservation.Date,
            reservation.StartTime,
            reservation.EndTime,
            reservation.Id);

        await _repository.UpdateAsync(id, reservation);
        return reservation;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var reservation = await _repository.GetByIdAsync(id);
        if (reservation is null) return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    private static readonly string[] DateFormats = { "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d", "dd/MM/yyyy", "d/M/yyyy" };
    private static readonly string[] TimeFormats = { "HH:mm", "H:mm", "H:m", "HH:m" };

    private static string NormalizeDate(string? date) =>
        DateOnly.TryParseExact((date ?? string.Empty).Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : throw new InvalidOperationException("Data inválida. Use o formato AAAA-MM-DD.");

    private static string NormalizeTime(string? time) =>
        TimeOnly.TryParseExact((time ?? string.Empty).Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value.ToString("HH:mm", CultureInfo.InvariantCulture)
            : throw new InvalidOperationException("Horário inválido. Use o formato HH:MM.");

    private static void EnsureReservationTimeIsValid(string startTime, string endTime, CommonArea commonArea)
    {
        if (string.Compare(startTime, endTime, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("O horário de início deve ser menor que o horário de término.");

        var outsideOpeningHours =
            string.Compare(startTime, commonArea.OpenTime, StringComparison.Ordinal) < 0 ||
            string.Compare(endTime, commonArea.CloseTime, StringComparison.Ordinal) > 0;

        if (outsideOpeningHours)
            throw new InvalidOperationException($"Esta área só funciona das {commonArea.OpenTime} às {commonArea.CloseTime}.");
    }

    // "Hoje" é o dia de Brasília, não o do relógio do servidor (UTC no Render).
    private void EnsureDateIsNotPast(string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var reservationDate))
            throw new InvalidOperationException("Data inválida.");

        if (reservationDate < BrazilTime.Today(_clock))
            throw new InvalidOperationException("Não é permitido registrar evento em data anterior a hoje.");
    }

    private async Task EnsureNoConflictAsync(
        string commonAreaId,
        string date,
        string startTime,
        string endTime,
        string? ignoredReservationId = null)
    {
        var reservations = await _repository.GetByCommonAreaAsync(commonAreaId);
        var hasConflict = reservations.Any(reservation =>
            reservation.Id != ignoredReservationId &&
            reservation.Date == date &&
            reservation.Status != ReservationStatus.Cancelada &&
            reservation.Status != ReservationStatus.Rejeitada &&
            string.Compare(startTime, reservation.EndTime, StringComparison.Ordinal) < 0 &&
            string.Compare(endTime, reservation.StartTime, StringComparison.Ordinal) > 0);

        if (hasConflict)
            throw new InvalidOperationException("Desculpe, este horário já está ocupado por outra reserva.");
    }
}

