using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Customers;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Customers;

public class CustomerService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : ICustomerService
{
    public async Task<PaginatedResult<CustomerListItem>> ListAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term) ||
                x.Email.Contains(term) ||
                x.Phone.Contains(term) ||
                (x.NationalId != null && x.NationalId.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CustomerListItem>
        {
            Items = items.Select(MapListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        var customer = await LoadCustomerAsync(id, asTracking: false, cancellationToken);
        return MapCustomer(customer);
    }

    public async Task<CustomerResponse> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateCustomerInput(request.FirstName, request.LastName, request.Email, request.Phone);

        var email = NormalizeEmail(request.Email);
        var nationalId = NormalizeOptional(request.NationalId);

        await EnsureEmailAvailableAsync(email, excludeId: null, cancellationToken);
        await EnsureNationalIdAvailableAsync(nationalId, excludeId: null, cancellationToken);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            NationalId = nationalId,
            DriverLicenseNumber = NormalizeOptional(request.DriverLicenseNumber),
            DriverLicenseExpiry = request.DriverLicenseExpiry,
            Address = NormalizeOptional(request.Address),
            Notes = NormalizeOptional(request.Notes),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(customer.Id, cancellationToken);
    }

    public async Task<CustomerResponse> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateCustomerInput(request.FirstName, request.LastName, request.Email, request.Phone);

        var customer = await LoadCustomerAsync(id, asTracking: true, cancellationToken);
        var email = NormalizeEmail(request.Email);
        var nationalId = NormalizeOptional(request.NationalId);

        await EnsureEmailAvailableAsync(email, excludeId: id, cancellationToken);
        await EnsureNationalIdAvailableAsync(nationalId, excludeId: id, cancellationToken);

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = email;
        customer.Phone = request.Phone.Trim();
        customer.NationalId = nationalId;
        customer.DriverLicenseNumber = NormalizeOptional(request.DriverLicenseNumber);
        customer.DriverLicenseExpiry = request.DriverLicenseExpiry;
        customer.Address = NormalizeOptional(request.Address);
        customer.Notes = NormalizeOptional(request.Notes);
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var customer = await LoadCustomerAsync(id, asTracking: true, cancellationToken);
        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerDocumentDto> AddDocumentAsync(
        Guid customerId,
        AddCustomerDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ValidationException("Document URL is required.");
        }

        if (request.Url.Trim().Length > 1000)
        {
            throw new ValidationException("Document URL is too long.");
        }

        var customer = await LoadCustomerAsync(customerId, asTracking: true, cancellationToken);

        var document = new CustomerDocument
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Type = request.Type,
            Url = request.Url.Trim(),
            FileName = NormalizeOptional(request.FileName),
            CreatedAt = DateTime.UtcNow
        };

        db.CustomerDocuments.Add(document);
        customer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapDocument(document);
    }

    public async Task DeleteDocumentAsync(
        Guid customerId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var customer = await LoadCustomerAsync(customerId, asTracking: true, cancellationToken);
        var document = customer.Documents.FirstOrDefault(x => x.Id == documentId)
            ?? throw new NotFoundException("Document not found.");

        db.CustomerDocuments.Remove(document);
        customer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> LoadCustomerAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = db.Customers
            .Include(x => x.Documents)
            .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Customer not found.");
    }

    private async Task EnsureEmailAvailableAsync(
        string email,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = db.Customers.Where(x =>
            x.TenantId == tenantContext.TenantId &&
            x.Email == email);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessException("A customer with this email already exists.");
        }
    }

    private async Task EnsureNationalIdAvailableAsync(
        string? nationalId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (nationalId is null)
        {
            return;
        }

        var query = db.Customers.Where(x =>
            x.TenantId == tenantContext.TenantId &&
            x.NationalId == nationalId);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessException("A customer with this national ID already exists.");
        }
    }

    private void EnsureCanRead()
    {
        if (!tenantContext.IsAuthenticated)
        {
            throw new UnauthorizedAppException("Tenant context is required.");
        }
    }

    private void EnsureCanWrite()
    {
        EnsureCanRead();

        if (tenantContext.Role is not (SystemRoles.Owner or SystemRoles.Admin or SystemRoles.Manager))
        {
            throw new ForbiddenException("You do not have permission to manage customers.");
        }
    }

    private static void ValidateCustomerInput(string firstName, string lastName, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ValidationException("Phone is required.");
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? MaskSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return $"{new string('*', Math.Min(6, trimmed.Length - 4))}{trimmed[^4..]}";
    }

    private static CustomerListItem MapListItem(Customer customer) => new()
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Email = customer.Email,
        Phone = customer.Phone,
        NationalIdMasked = MaskSensitive(customer.NationalId),
        DriverLicenseNumberMasked = MaskSensitive(customer.DriverLicenseNumber),
        IsActive = customer.IsActive,
        CreatedAt = customer.CreatedAt
    };

    private static CustomerResponse MapCustomer(Customer customer) => new()
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Email = customer.Email,
        Phone = customer.Phone,
        NationalId = customer.NationalId,
        DriverLicenseNumber = customer.DriverLicenseNumber,
        DriverLicenseExpiry = customer.DriverLicenseExpiry,
        Address = customer.Address,
        Notes = customer.Notes,
        IsActive = customer.IsActive,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt,
        Documents = customer.Documents
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapDocument)
            .ToList()
    };

    private static CustomerDocumentDto MapDocument(CustomerDocument document) => new()
    {
        Id = document.Id,
        Type = document.Type.ToString(),
        Url = document.Url,
        FileName = document.FileName,
        CreatedAt = document.CreatedAt
    };
}
