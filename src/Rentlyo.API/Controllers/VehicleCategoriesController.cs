using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Vehicles;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicle-categories")]
public class VehicleCategoriesController(IVehicleCategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleCategoryResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var data = await categoryService.ListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<VehicleCategoryResponse>>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VehicleCategoryResponse>>> Create(
        [FromBody] CreateVehicleCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var data = await categoryService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<VehicleCategoryResponse>.Success(data, "Category created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VehicleCategoryResponse>>> Update(
        Guid id,
        [FromBody] UpdateVehicleCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var data = await categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<VehicleCategoryResponse>.Success(data, "Category updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Category deleted."));
    }
}
