namespace Rentlyo.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Other = 4
}

public enum RefundStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public enum PaymentTransactionType
{
    Created = 1,
    MarkedPaid = 2,
    WebhookPaid = 3,
    Failed = 4,
    Refunded = 5
}
