using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Reservations;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Reservations;

public class ReservationService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : IReservationService
{
    private static readonly ReservationStatus[] ActiveStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed
    ];

    public async Task<PaginatedResult<ReservationListItem>> ListAsync(
        ReservationStatus? status,
        Guid? vehicleId,
        Guid? customerId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Reservations
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Customer)
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.EndAt > from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.StartAt < to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ReservationListItem>
        {
            Items = items.Select(MapListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        var reservation = await LoadReservationAsync(id, asTracking: false, cancellationToken);
        return Map(reservation);
    }

    public async Task<ReservationResponse> CreateAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        NormalizeRange(request.StartAt, request.EndAt, out var startAt, out var endAt);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var vehicle = await LoadBookableVehicleAsync(request.VehicleId, cancellationToken);
        await EnsureCustomerAsync(request.CustomerId, cancellationToken);
        await EnsurePickupLocationAsync(request.PickupLocationId, cancellationToken);
        await EnsureDropoffLocationAsync(request.DropoffLocationId, cancellationToken);
        await EnsureNoOverlapAsync(request.VehicleId, startAt, endAt, excludeId: null, cancellationToken);

        var billableDays = CalculateBillableDays(startAt, endAt);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            VehicleId = vehicle.Id,
            CustomerId = request.CustomerId,
            PickupLocationId = request.PickupLocationId,
            DropoffLocationId = request.DropoffLocationId,
            StartAt = startAt,
            EndAt = endAt,
            Status = ReservationStatus.Pending,
            DailyPriceSnapshot = vehicle.DailyPrice,
            TotalPrice = vehicle.DailyPrice * billableDays,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(reservation.Id, cancellationToken);
    }

    public async Task<ReservationResponse> UpdateAsync(
        Guid id,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        NormalizeRange(request.StartAt, request.EndAt, out var startAt, out var endAt);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var reservation = await LoadReservationAsync(id, asTracking: true, cancellationToken);
        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Completed)
        {
            throw new BusinessException("Cancelled or completed reservations cannot be updated.");
        }

        var vehicle = await LoadBookableVehicleAsync(request.VehicleId, cancellationToken);
        await EnsureCustomerAsync(request.CustomerId, cancellationToken);
        await EnsurePickupLocationAsync(request.PickupLocationId, cancellationToken);
        await EnsureDropoffLocationAsync(request.DropoffLocationId, cancellationToken);
        await EnsureNoOverlapAsync(request.VehicleId, startAt, endAt, excludeId: id, cancellationToken);

        var billableDays = CalculateBillableDays(startAt, endAt);
        reservation.VehicleId = vehicle.Id;
        reservation.CustomerId = request.CustomerId;
        reservation.PickupLocationId = request.PickupLocationId;
        reservation.DropoffLocationId = request.DropoffLocationId;
        reservation.StartAt = startAt;
        reservation.EndAt = endAt;
        reservation.DailyPriceSnapshot = vehicle.DailyPrice;
        reservation.TotalPrice = vehicle.DailyPrice * billableDays;
        reservation.Notes = NormalizeOptional(request.Notes);
        reservation.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReservationResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var reservation = await LoadReservationAsync(id, asTracking: true, cancellationToken);
        if (reservation.Status != ReservationStatus.Pending)
        {
            throw new BusinessException("Only pending reservations can be confirmed.");
        }

        await EnsureNoOverlapAsync(
            reservation.VehicleId,
            reservation.StartAt,
            reservation.EndAt,
            excludeId: id,
            cancellationToken);

        reservation.Status = ReservationStatus.Confirmed;
        reservation.ConfirmedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReservationResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var reservation = await LoadReservationAsync(id, asTracking: true, cancellationToken);
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
        {
            throw new BusinessException("Only pending or confirmed reservations can be cancelled.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReservationResponse> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var reservation = await LoadReservationAsync(id, asTracking: true, cancellationToken);
        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new BusinessException("Only confirmed reservations can be completed.");
        }

        reservation.Status = ReservationStatus.Completed;
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<AvailabilityResponse> CheckAvailabilityAsync(
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        Guid? excludeReservationId,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        try
        {
            NormalizeRange(startAt, endAt, out startAt, out endAt);
        }
        catch (ValidationException ex)
        {
            return new AvailabilityResponse { IsAvailable = false, Reason = ex.Message };
        }

        var vehicle = await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == vehicleId && x.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (vehicle is null)
        {
            return new AvailabilityResponse { IsAvailable = false, Reason = "Vehicle not found." };
        }

        if (vehicle.Status is VehicleStatus.Maintenance or VehicleStatus.Inactive)
        {
            return new AvailabilityResponse
            {
                IsAvailable = false,
                Reason = $"Vehicle is {vehicle.Status}."
            };
        }

        var hasOverlap = await HasOverlapAsync(
            vehicleId,
            startAt,
            endAt,
            excludeReservationId,
            cancellationToken);

        if (hasOverlap)
        {
            return new AvailabilityResponse
            {
                IsAvailable = false,
                Reason = "Vehicle is not available for the selected dates."
            };
        }

        var billableDays = CalculateBillableDays(startAt, endAt);
        return new AvailabilityResponse
        {
            IsAvailable = true,
            BillableDays = billableDays,
            EstimatedTotalPrice = vehicle.DailyPrice * billableDays
        };
    }

    public async Task<IReadOnlyList<ReservationCalendarItem>> GetCalendarAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        if (to <= from)
        {
            throw new ValidationException("Calendar end must be after start.");
        }

        var items = await db.Reservations
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Customer)
            .Where(x =>
                x.TenantId == tenantContext.TenantId &&
                x.Status != ReservationStatus.Cancelled &&
                x.StartAt < to &&
                x.EndAt > from)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);

        return items.Select(x => new ReservationCalendarItem
        {
            Id = x.Id,
            VehicleId = x.VehicleId,
            VehicleLabel = $"{x.Vehicle.Brand} {x.Vehicle.Model}",
            CustomerName = $"{x.Customer.FirstName} {x.Customer.LastName}",
            StartAt = x.StartAt,
            EndAt = x.EndAt,
            Status = x.Status.ToString()
        }).ToList();
    }

    private async Task EnsureNoOverlapAsync(
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (await HasOverlapAsync(vehicleId, startAt, endAt, excludeId, cancellationToken))
        {
            throw new BusinessException("Vehicle already has an overlapping reservation.");
        }
    }

    private async Task<bool> HasOverlapAsync(
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations.Where(x =>
            x.TenantId == tenantContext.TenantId &&
            x.VehicleId == vehicleId &&
            ActiveStatuses.Contains(x.Status) &&
            x.StartAt < endAt &&
            x.EndAt > startAt);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task<Vehicle> LoadBookableVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles
            .FirstOrDefaultAsync(
                x => x.Id == vehicleId && x.TenantId == tenantContext.TenantId,
                cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");

        if (vehicle.Status is VehicleStatus.Maintenance or VehicleStatus.Inactive)
        {
            throw new BusinessException($"Vehicle is {vehicle.Status} and cannot be reserved.");
        }

        return vehicle;
    }

    private async Task EnsureCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var exists = await db.Customers.AnyAsync(
            x => x.Id == customerId && x.TenantId == tenantContext.TenantId && x.IsActive,
            cancellationToken);

        if (!exists)
        {
            throw new ValidationException("Customer not found or inactive.");
        }
    }

    private async Task EnsurePickupLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await db.Locations.FirstOrDefaultAsync(
            x => x.Id == locationId && x.TenantId == tenantContext.TenantId && x.IsActive,
            cancellationToken)
            ?? throw new ValidationException("Pickup location not found or inactive.");

        if (location.Type is not (LocationType.Branch or LocationType.Pickup))
        {
            throw new ValidationException("Pickup location must be a Branch or Pickup type.");
        }
    }

    private async Task EnsureDropoffLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await db.Locations.FirstOrDefaultAsync(
            x => x.Id == locationId && x.TenantId == tenantContext.TenantId && x.IsActive,
            cancellationToken)
            ?? throw new ValidationException("Dropoff location not found or inactive.");

        if (location.Type is not (LocationType.Branch or LocationType.Dropoff))
        {
            throw new ValidationException("Dropoff location must be a Branch or Dropoff type.");
        }
    }

    private async Task<Reservation> LoadReservationAsync(
        Guid id,
        bool asTracking,
        CancellationToken cancellationToken)
    {
        IQueryable<Reservation> query = db.Reservations
            .Include(x => x.Vehicle)
            .Include(x => x.Customer)
            .Include(x => x.PickupLocation)
            .Include(x => x.DropoffLocation)
            .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Reservation not found.");
    }

    private void EnsureCanRead()
    {
        if (!tenantContext.IsAuthenticated)
        {
            throw new UnauthorizedAppException("Tenant context is required.");
        }
    }

    private void EnsureCanWrite()
    {
        EnsureCanRead();

        if (tenantContext.Role is not (SystemRoles.Owner or SystemRoles.Admin or SystemRoles.Manager))
        {
            throw new ForbiddenException("You do not have permission to manage reservations.");
        }
    }

    private static void NormalizeRange(
        DateTime startAt,
        DateTime endAt,
        out DateTime normalizedStart,
        out DateTime normalizedEnd)
    {
        normalizedStart = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        normalizedEnd = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);

        if (normalizedEnd <= normalizedStart)
        {
            throw new ValidationException("End must be after start.");
        }
    }

    private static int CalculateBillableDays(DateTime startAt, DateTime endAt)
    {
        var days = (int)Math.Ceiling((endAt - startAt).TotalDays);
        return Math.Max(1, days);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ReservationListItem MapListItem(Reservation reservation) => new()
    {
        Id = reservation.Id,
        VehicleId = reservation.VehicleId,
        VehicleLabel = $"{reservation.Vehicle.Brand} {reservation.Vehicle.Model}",
        CustomerId = reservation.CustomerId,
        CustomerName = $"{reservation.Customer.FirstName} {reservation.Customer.LastName}",
        StartAt = reservation.StartAt,
        EndAt = reservation.EndAt,
        Status = reservation.Status.ToString(),
        TotalPrice = reservation.TotalPrice,
        CreatedAt = reservation.CreatedAt
    };

    private static ReservationResponse Map(Reservation reservation) => new()
    {
        Id = reservation.Id,
        VehicleId = reservation.VehicleId,
        VehicleLabel = $"{reservation.Vehicle.Brand} {reservation.Vehicle.Model}",
        CustomerId = reservation.CustomerId,
        CustomerName = $"{reservation.Customer.FirstName} {reservation.Customer.LastName}",
        PickupLocationId = reservation.PickupLocationId,
        PickupLocationName = reservation.PickupLocation.Name,
        DropoffLocationId = reservation.DropoffLocationId,
        DropoffLocationName = reservation.DropoffLocation.Name,
        StartAt = reservation.StartAt,
        EndAt = reservation.EndAt,
        Status = reservation.Status.ToString(),
        DailyPriceSnapshot = reservation.DailyPriceSnapshot,
        TotalPrice = reservation.TotalPrice,
        Notes = reservation.Notes,
        CreatedAt = reservation.CreatedAt,
        UpdatedAt = reservation.UpdatedAt,
        CancelledAt = reservation.CancelledAt,
        ConfirmedAt = reservation.ConfirmedAt
    };
}
