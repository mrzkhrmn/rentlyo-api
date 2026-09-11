using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicController(
    IPublicCatalogService publicCatalogService,
    ILeadService leadService,
    ITenantService tenantService) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanResponse>>>> ListPlans(
        CancellationToken cancellationToken)
    {
        var data = await tenantService.ListPlansPublicAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlanResponse>>.Success(data));
    }

    [HttpPost("leads")]
    public async Task<ActionResult<ApiResponse<LeadResponse>>> CreateLead(
        [FromBody] CreateLeadRequest request,
        CancellationToken cancellationToken)
    {
        var data = await leadService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<LeadResponse>.Success(data, "Request received."));
    }

    [HttpGet("tenant/{slug}")]
    public async Task<ActionResult<ApiResponse<PublicTenantResponse>>> GetTenant(
        string slug,
        CancellationToken cancellationToken)
    {
        var data = await publicCatalogService.GetTenantAsync(slug, cancellationToken);
        return Ok(ApiResponse<PublicTenantResponse>.Success(data));
    }

    [HttpGet("{slug}/vehicles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicVehicleListItem>>>> ListVehicles(
        string slug,
        CancellationToken cancellationToken)
    {
        var data = await publicCatalogService.ListVehiclesAsync(slug, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PublicVehicleListItem>>.Success(data));
    }

    [HttpGet("{slug}/vehicles/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PublicVehicleDetail>>> GetVehicle(
        string slug,
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await publicCatalogService.GetVehicleAsync(slug, id, cancellationToken);
        return Ok(ApiResponse<PublicVehicleDetail>.Success(data));
    }

    [HttpGet("{slug}/locations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicLocationItem>>>> ListLocations(
        string slug,
        CancellationToken cancellationToken)
    {
        var data = await publicCatalogService.ListLocationsAsync(slug, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PublicLocationItem>>.Success(data));
    }

    [HttpGet("{slug}/availability")]
    public async Task<ActionResult<ApiResponse<PublicAvailabilityResponse>>> Availability(
        string slug,
        [FromQuery] Guid vehicleId,
        [FromQuery] DateTime startAt,
        [FromQuery] DateTime endAt,
        CancellationToken cancellationToken)
    {
        var data = await publicCatalogService.CheckAvailabilityAsync(
            slug,
            vehicleId,
            startAt,
            endAt,
            cancellationToken);
        return Ok(ApiResponse<PublicAvailabilityResponse>.Success(data));
    }

    [HttpPost("{slug}/reservations")]
    public async Task<ActionResult<ApiResponse<PublicReservationResponse>>> CreateReservation(
        string slug,
        [FromBody] PublicCreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var data = await publicCatalogService.CreateReservationAsync(slug, request, cancellationToken);
        return Ok(ApiResponse<PublicReservationResponse>.Success(data, "Reservation request received."));
    }
}
