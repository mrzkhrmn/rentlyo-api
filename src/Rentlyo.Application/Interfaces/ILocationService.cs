using Rentlyo.Application.DTOs.Locations;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface ILocationService
{
    Task<PaginatedResult<LocationListItem>> ListAsync(
        LocationType? type,
        bool? isActive,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LocationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);

    Task<LocationResponse> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
