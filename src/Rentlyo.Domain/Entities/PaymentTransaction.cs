using Rentlyo.Domain.Enums;

namespace Rentlyo.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public PaymentTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment Payment { get; set; } = null!;
}
