using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant")]
public class TenantController(ITenantService tenantService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> GetCurrent(CancellationToken cancellationToken)
    {
        var data = await tenantService.GetCurrentAsync(cancellationToken);
        return Ok(ApiResponse<TenantResponse>.Success(data));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> UpdateProfile(
        [FromBody] UpdateTenantProfileRequest request,
        CancellationToken cancellationToken)
    {
        var data = await tenantService.UpdateProfileAsync(request, cancellationToken);
        return Ok(ApiResponse<TenantResponse>.Success(data, "Profile updated."));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<TenantSettingsResponse>>> GetSettings(
        CancellationToken cancellationToken)
    {
        var data = await tenantService.GetSettingsAsync(cancellationToken);
        return Ok(ApiResponse<TenantSettingsResponse>.Success(data));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<TenantSettingsResponse>>> UpdateSettings(
        [FromBody] UpdateTenantSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var data = await tenantService.UpdateSettingsAsync(request, cancellationToken);
        return Ok(ApiResponse<TenantSettingsResponse>.Success(data, "Settings updated."));
    }

    [HttpPut("plan")]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> ChangePlan(
        [FromBody] ChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        var data = await tenantService.ChangePlanAsync(request, cancellationToken);
        return Ok(ApiResponse<TenantResponse>.Success(data, "Plan updated."));
    }
}
