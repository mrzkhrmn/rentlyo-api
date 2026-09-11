using Rentlyo.Domain.Enums;

namespace Rentlyo.Application.DTOs.Customers;

public class CustomerDocumentDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerListItem
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? NationalIdMasked { get; set; }
    public string? DriverLicenseNumberMasked { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? DriverLicenseNumber { get; set; }
    public DateOnly? DriverLicenseExpiry { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<CustomerDocumentDto> Documents { get; set; } = [];
}

public class CreateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? DriverLicenseNumber { get; set; }
    public DateOnly? DriverLicenseExpiry { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? DriverLicenseNumber { get; set; }
    public DateOnly? DriverLicenseExpiry { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class AddCustomerDocumentRequest
{
    public CustomerDocumentType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
}
