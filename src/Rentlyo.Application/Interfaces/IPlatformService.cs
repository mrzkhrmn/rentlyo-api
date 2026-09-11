using Rentlyo.Application.DTOs.Platform;
using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface IPlatformService
{
    Task EnsureSeedAdminAsync(CancellationToken cancellationToken = default);

    Task<PlatformAuthResponse> LoginAsync(
        PlatformLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<PlatformTenantListItem>> ListTenantsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantDetail> GetTenantAsync(Guid id, CancellationToken cancellationToken = default);

    Task SuspendTenantAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActivateTenantAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetWebsiteEnabledAsync(
        Guid id,
        SetWebsiteEnabledRequest request,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<LeadResponse>> ListLeadsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LeadResponse> UpdateLeadStatusAsync(
        Guid id,
        UpdateLeadStatusRequest request,
        CancellationToken cancellationToken = default);
}

public interface ILeadService
{
    Task<LeadResponse> CreateAsync(CreateLeadRequest request, CancellationToken cancellationToken = default);
}
