using Rentlyo.Application.DTOs.Public;

namespace Rentlyo.Application.Interfaces;

public interface IPublicCatalogService
{
    Task<PublicTenantResponse> GetTenantAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicVehicleListItem>> ListVehiclesAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<PublicVehicleDetail> GetVehicleAsync(
        string slug,
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicLocationItem>> ListLocationsAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<PublicAvailabilityResponse> CheckAvailabilityAsync(
        string slug,
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default);

    Task<PublicReservationResponse> CreateReservationAsync(
        string slug,
        PublicCreateReservationRequest request,
        CancellationToken cancellationToken = default);
}
