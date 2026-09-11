using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Dashboard;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.Infrastructure.Dashboard;

public class DashboardService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!tenantContext.IsAuthenticated)
        {
            throw new UnauthorizedAppException("Tenant context is required.");
        }

        var tenantId = tenantContext.TenantId;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var daysInMonth = (monthEnd - monthStart).Days;

        var totalVehicles = await db.Vehicles
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var availableVehicles = await db.Vehicles
            .CountAsync(
                x => x.TenantId == tenantId && x.Status == VehicleStatus.Available,
                cancellationToken);

        var activeReservations = await db.Reservations
            .CountAsync(
                x => x.TenantId == tenantId
                     && x.Status == ReservationStatus.Confirmed
                     && x.StartAt <= now
                     && x.EndAt > now,
                cancellationToken);

        var upcomingQuery = db.Reservations
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Customer)
            .Where(x =>
                x.TenantId == tenantId
                && (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed)
                && x.StartAt > now);

        var upcomingReservations = await upcomingQuery.CountAsync(cancellationToken);

        var upcomingItems = await upcomingQuery
            .OrderBy(x => x.StartAt)
            .Take(5)
            .Select(x => new DashboardReservationItem
            {
                Id = x.Id,
                VehicleLabel = x.Vehicle.Brand + " " + x.Vehicle.Model,
                CustomerName = x.Customer.FirstName + " " + x.Customer.LastName,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                Status = x.Status.ToString(),
                TotalPrice = x.TotalPrice
            })
            .ToListAsync(cancellationToken);

        var revenueThisMonth = await db.Reservations
            .Where(x =>
                x.TenantId == tenantId
                && (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.Completed)
                && x.StartAt < monthEnd
                && x.EndAt > monthStart)
            .SumAsync(x => (decimal?)x.TotalPrice, cancellationToken) ?? 0m;

        var paidRevenueThisMonth = await db.Payments
            .Where(x =>
                x.TenantId == tenantId
                && x.Status == PaymentStatus.Paid
                && x.PaidAt != null
                && x.PaidAt >= monthStart
                && x.PaidAt < monthEnd)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var occupancyReservations = await db.Reservations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId
                && x.Status != ReservationStatus.Cancelled
                && x.StartAt < monthEnd
                && x.EndAt > monthStart)
            .Select(x => new { x.StartAt, x.EndAt })
            .ToListAsync(cancellationToken);

        var bookedVehicleDays = occupancyReservations.Sum(x =>
        {
            var overlapStart = x.StartAt > monthStart ? x.StartAt : monthStart;
            var overlapEnd = x.EndAt < monthEnd ? x.EndAt : monthEnd;
            var days = (overlapEnd - overlapStart).TotalDays;
            return Math.Max(0, days);
        });

        var capacityDays = totalVehicles * daysInMonth;
        var occupancyPercent = capacityDays <= 0
            ? 0m
            : Math.Clamp((decimal)(bookedVehicleDays / capacityDays * 100d), 0m, 100m);

        var currency = await db.TenantSettings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "TRY";

        return new DashboardSummaryResponse
        {
            TotalVehicles = totalVehicles,
            AvailableVehicles = availableVehicles,
            ActiveReservations = activeReservations,
            UpcomingReservations = upcomingReservations,
            RevenueThisMonth = revenueThisMonth,
            PaidRevenueThisMonth = paidRevenueThisMonth,
            OccupancyPercent = Math.Round(occupancyPercent, 1),
            Currency = currency,
            UpcomingItems = upcomingItems
        };
    }
}
