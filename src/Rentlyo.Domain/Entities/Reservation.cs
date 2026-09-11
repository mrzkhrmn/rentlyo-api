using Rentlyo.Domain.Enums;
using Rentlyo.Domain.Interfaces;

namespace Rentlyo.Domain.Entities;

public class Reservation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PickupLocationId { get; set; }
    public Guid DropoffLocationId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public decimal DailyPriceSnapshot { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Location PickupLocation { get; set; } = null!;
    public Location DropoffLocation { get; set; } = null!;
}
