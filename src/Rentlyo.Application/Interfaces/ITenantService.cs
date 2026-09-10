using Rentlyo.Application.DTOs.Tenant;

namespace Rentlyo.Application.Interfaces;

public interface ITenantService
{
    Task<TenantResponse> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<TenantResponse> UpdateProfileAsync(UpdateTenantProfileRequest request, CancellationToken cancellationToken = default);
    Task<TenantSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<TenantSettingsResponse> UpdateSettingsAsync(UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanResponse>> ListPlansAsync(CancellationToken cancellationToken = default);
    Task<TenantResponse> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default);
}
