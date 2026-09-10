namespace Rentlyo.Application.DTOs.Tenant;

public class PlanResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxVehicles { get; set; }
    public int MaxUsers { get; set; }
}

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? LogoUrl { get; set; }
    public PlanResponse Plan { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class UpdateTenantProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? LogoUrl { get; set; }
}

public class TenantSettingsResponse
{
    public string Timezone { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
}

public class UpdateTenantSettingsRequest
{
    public string Timezone { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
}

public class ChangePlanRequest
{
    public Guid PlanId { get; set; }
}
