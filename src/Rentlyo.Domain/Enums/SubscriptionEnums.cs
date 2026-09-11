namespace Rentlyo.Domain.Enums;

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Cancelled = 4,
    Expired = 5
}

public enum SubscriptionPaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3
}
