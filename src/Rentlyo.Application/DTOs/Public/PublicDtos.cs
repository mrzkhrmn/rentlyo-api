namespace Rentlyo.Application.DTOs.Public;

public class PublicTenantResponse
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string PrimaryColor { get; set; } = "#0F766E";
    public string AccentColor { get; set; } = "#F59E0B";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class PublicVehicleListItem
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal DailyPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class PublicVehicleDetail : PublicVehicleListItem
{
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
}

public class PublicLocationItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class PublicAvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
    public decimal? EstimatedTotalPrice { get; set; }
    public int? BillableDays { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class PublicCreateReservationRequest
{
    public Guid VehicleId { get; set; }
    public Guid PickupLocationId { get; set; }
    public Guid DropoffLocationId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class PublicReservationResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
}
