using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Platform;
using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Route("api/platform")]
public class PlatformController(IPlatformService platformService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<ActionResult<ApiResponse<PlatformAuthResponse>>> Login(
        [FromBody] PlatformLoginRequest request,
        CancellationToken cancellationToken)
    {
        var data = await platformService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformAuthResponse>.Success(data));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpGet("tenants")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<PlatformTenantListItem>>>> ListTenants(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await platformService.ListTenantsAsync(search, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PlatformTenantListItem>>.Success(data));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpGet("tenants/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PlatformTenantDetail>>> GetTenant(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await platformService.GetTenantAsync(id, cancellationToken);
        return Ok(ApiResponse<PlatformTenantDetail>.Success(data));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpPost("tenants/{id:guid}/suspend")]
    public async Task<ActionResult<ApiResponse<object?>>> Suspend(
        Guid id,
        CancellationToken cancellationToken)
    {
        await platformService.SuspendTenantAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Tenant suspended."));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpPost("tenants/{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<object?>>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await platformService.ActivateTenantAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Tenant activated."));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpPost("tenants/{id:guid}/website")]
    public async Task<ActionResult<ApiResponse<object?>>> SetWebsite(
        Guid id,
        [FromBody] SetWebsiteEnabledRequest request,
        CancellationToken cancellationToken)
    {
        await platformService.SetWebsiteEnabledAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Website flag updated."));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpGet("leads")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<LeadResponse>>>> ListLeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await platformService.ListLeadsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<LeadResponse>>.Success(data));
    }

    [Authorize(Roles = "PlatformAdmin")]
    [HttpPatch("leads/{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeadResponse>>> UpdateLead(
        Guid id,
        [FromBody] UpdateLeadStatusRequest request,
        CancellationToken cancellationToken)
    {
        var data = await platformService.UpdateLeadStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LeadResponse>.Success(data, "Lead updated."));
    }
}
