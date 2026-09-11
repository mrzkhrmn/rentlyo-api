using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Locations;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/locations")]
public class LocationsController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<LocationListItem>>>> List(
        [FromQuery] LocationType? type,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await locationService.ListAsync(type, isActive, search, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<LocationListItem>>.Success(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await locationService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<LocationResponse>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> Create(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var data = await locationService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<LocationResponse>.Success(data, "Location created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> Update(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var data = await locationService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LocationResponse>.Success(data, "Location updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await locationService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Location deleted."));
    }
}
