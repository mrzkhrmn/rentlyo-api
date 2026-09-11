using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.DTOs.Platform;

public class PlatformLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class PlatformAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public PlatformUserDto User { get; set; } = null!;
}

public class PlatformUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class PlatformTenantListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string? SubscriptionStatus { get; set; }
    public bool WebsiteEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlatformTenantDetail : PlatformTenantListItem
{
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public int VehicleCount { get; set; }
    public int ReservationCount { get; set; }
}

public class UpdateLeadStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SetWebsiteEnabledRequest
{
    public bool WebsiteEnabled { get; set; }
}
