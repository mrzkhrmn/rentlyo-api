using Rentlyo.Domain.Enums;

namespace Rentlyo.Application.DTOs.Reservations;

public class ReservationListItem
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReservationResponse
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid PickupLocationId { get; set; }
    public string PickupLocationName { get; set; } = string.Empty;
    public Guid DropoffLocationId { get; set; }
    public string DropoffLocationName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal DailyPriceSnapshot { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class CreateReservationRequest
{
    public Guid VehicleId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PickupLocationId { get; set; }
    public Guid DropoffLocationId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateReservationRequest
{
    public Guid VehicleId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PickupLocationId { get; set; }
    public Guid DropoffLocationId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Notes { get; set; }
}

public class AvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
    public decimal? EstimatedTotalPrice { get; set; }
    public int? BillableDays { get; set; }
}

public class ReservationCalendarItem
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
