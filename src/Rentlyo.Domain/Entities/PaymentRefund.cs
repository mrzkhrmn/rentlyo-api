using Rentlyo.Domain.Enums;

namespace Rentlyo.Domain.Entities;

public class PaymentRefund
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Completed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment Payment { get; set; } = null!;
}
