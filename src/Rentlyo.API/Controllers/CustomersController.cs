using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Customers;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CustomerListItem>>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await customerService.ListAsync(search, isActive, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<CustomerListItem>>.Success(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await customerService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<CustomerResponse>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var data = await customerService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<CustomerResponse>.Success(data, "Customer created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var data = await customerService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CustomerResponse>.Success(data, "Customer updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await customerService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Customer deleted."));
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<ActionResult<ApiResponse<CustomerDocumentDto>>> AddDocument(
        Guid id,
        [FromBody] AddCustomerDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var data = await customerService.AddDocumentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CustomerDocumentDto>.Success(data, "Document added."));
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await customerService.DeleteDocumentAsync(id, documentId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Document deleted."));
    }
}
