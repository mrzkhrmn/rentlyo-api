using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Employees;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<EmployeeListItem>>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await employeeService.ListAsync(search, isActive, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<EmployeeListItem>>.Success(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await employeeService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeResponse>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var data = await employeeService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<EmployeeResponse>.Success(data, "Employee created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var data = await employeeService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeResponse>.Success(data, "Employee updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await employeeService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Employee deactivated."));
    }
}
