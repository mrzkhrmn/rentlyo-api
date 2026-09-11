using Rentlyo.Domain.Enums;

namespace Rentlyo.Application.DTOs.Payments;

public class PaymentListItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string ReservationLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class PaymentRefundDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string ReservationLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? ExternalCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public IReadOnlyList<PaymentRefundDto> Refunds { get; set; } = [];
    public IReadOnlyList<PaymentTransactionDto> Transactions { get; set; } = [];
}

public class CreatePaymentRequest
{
    public Guid ReservationId { get; set; }
    public decimal? Amount { get; set; }
    public PaymentMethod? Method { get; set; }
}

public class RecordPaymentRequest
{
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Note { get; set; }
}

public class RefundPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class PaymentWebhookRequest
{
    public string ProviderReference { get; set; } = string.Empty;
    public string EventType { get; set; } = "payment.paid";
}
