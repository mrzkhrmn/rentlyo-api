namespace Rentlyo.Domain.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxVehicles { get; set; }
    public int MaxUsers { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;

    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}
