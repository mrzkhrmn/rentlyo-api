using Rentlyo.Application.DTOs.Vehicles;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface IVehicleService
{
    Task<PaginatedResult<VehicleListItem>> ListAsync(
        VehicleStatus? status,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<VehicleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VehicleResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleResponse> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VehicleImageDto> AddImageAsync(
        Guid vehicleId,
        AddVehicleImageRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(Guid vehicleId, Guid imageId, CancellationToken cancellationToken = default);
}

public interface IVehicleCategoryService
{
    Task<IReadOnlyList<VehicleCategoryResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<VehicleCategoryResponse> CreateAsync(
        CreateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleCategoryResponse> UpdateAsync(
        Guid id,
        UpdateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
