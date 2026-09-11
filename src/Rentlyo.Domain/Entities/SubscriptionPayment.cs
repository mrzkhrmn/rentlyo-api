using Rentlyo.Domain.Enums;

namespace Rentlyo.Domain.Entities;

public class SubscriptionPayment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public SubscriptionPaymentStatus Status { get; set; } = SubscriptionPaymentStatus.Pending;
    public string? ProviderReference { get; set; }
    public string? ExternalCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
}
