using Rentlyo.Domain.Enums;

namespace Rentlyo.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SubscriptionPlan Plan { get; set; } = null!;
    public TenantSettings Settings { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
}
