using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Vehicles;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<VehicleListItem>>>> List(
        [FromQuery] VehicleStatus? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await vehicleService.ListAsync(status, categoryId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<VehicleListItem>>.Success(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await vehicleService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<VehicleResponse>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var data = await vehicleService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<VehicleResponse>.Success(data, "Vehicle created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var data = await vehicleService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<VehicleResponse>.Success(data, "Vehicle updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await vehicleService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Vehicle deleted."));
    }

    [HttpPost("{id:guid}/images")]
    public async Task<ActionResult<ApiResponse<VehicleImageDto>>> AddImage(
        Guid id,
        [FromBody] AddVehicleImageRequest request,
        CancellationToken cancellationToken)
    {
        var data = await vehicleService.AddImageAsync(id, request, cancellationToken);
        return Ok(ApiResponse<VehicleImageDto>.Success(data, "Image added."));
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteImage(
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        await vehicleService.DeleteImageAsync(id, imageId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Image deleted."));
    }
}
