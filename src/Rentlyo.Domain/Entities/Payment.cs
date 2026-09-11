using Rentlyo.Domain.Enums;
using Rentlyo.Domain.Interfaces;

namespace Rentlyo.Domain.Entities;

public class Payment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Other;
    public string? ProviderReference { get; set; }
    public string? ExternalCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Reservation Reservation { get; set; } = null!;
    public ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}
