using Rentlyo.Application.DTOs.Reservations;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface IReservationService
{
    Task<PaginatedResult<ReservationListItem>> ListAsync(
        ReservationStatus? status,
        Guid? vehicleId,
        Guid? customerId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReservationResponse> CreateAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse> UpdateAsync(
        Guid id,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReservationResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReservationResponse> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AvailabilityResponse> CheckAvailabilityAsync(
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        Guid? excludeReservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationCalendarItem>> GetCalendarAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
