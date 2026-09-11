using Rentlyo.Application.DTOs.Tenant;

namespace Rentlyo.Application.DTOs.Subscriptions;

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public PlanResponse Plan { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? PendingPlanId { get; set; }
    public PlanResponse? PendingPlan { get; set; }
    public SubscriptionPaymentListItem? LatestPendingPayment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SubscriptionPaymentListItem
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? ExternalCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangeSubscriptionPlanRequest
{
    public Guid PlanId { get; set; }
}

public class ChangeSubscriptionPlanResult
{
    public SubscriptionResponse Subscription { get; set; } = null!;
    public SubscriptionPaymentListItem? Payment { get; set; }
    public bool RequiresPayment { get; set; }
}

public class RecordSubscriptionPaymentRequest
{
    public string Method { get; set; } = "BankTransfer";
    public string? Note { get; set; }
}

public class SubscriptionWebhookRequest
{
    public string ProviderReference { get; set; } = string.Empty;
    public string EventType { get; set; } = "subscription.paid";
}
