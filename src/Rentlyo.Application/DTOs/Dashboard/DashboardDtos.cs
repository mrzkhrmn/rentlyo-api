namespace Rentlyo.Application.DTOs.Dashboard;

public class DashboardReservationItem
{
    public Guid Id { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
}

public class DashboardSummaryResponse
{
    public int TotalVehicles { get; set; }
    public int AvailableVehicles { get; set; }
    public int ActiveReservations { get; set; }
    public int UpcomingReservations { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal PaidRevenueThisMonth { get; set; }
    public decimal OccupancyPercent { get; set; }
    public string Currency { get; set; } = "TRY";
    public IReadOnlyList<DashboardReservationItem> UpcomingItems { get; set; } = [];
}
