using Rentlyo.Application.DTOs.Customers;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface ICustomerService
{
    Task<PaginatedResult<CustomerListItem>> ListAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerDocumentDto> AddDocumentAsync(
        Guid customerId,
        AddCustomerDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid customerId, Guid documentId, CancellationToken cancellationToken = default);
}
