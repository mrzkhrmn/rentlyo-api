using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.Infrastructure.Public;

public class PublicCatalogService(ApplicationDbContext db) : IPublicCatalogService
{
    private static readonly ReservationStatus[] ActiveStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed
    ];

    private static readonly Regex HexColor = new(
        "^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$",
        RegexOptions.Compiled);

    public async Task<PublicTenantResponse> GetTenantAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(slug, cancellationToken);
        return MapTenant(tenant);
    }

    public async Task<IReadOnlyList<PublicVehicleListItem>> ListVehiclesAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(slug, cancellationToken);

        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.TenantId == tenant.Id && x.Status == VehicleStatus.Available)
            .OrderBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .ToListAsync(cancellationToken);

        return vehicles.Select(MapVehicleListItem).ToList();
    }

    public async Task<PublicVehicleDetail> GetVehicleAsync(
        string slug,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(slug, cancellationToken);

        var vehicle = await db.Vehicles
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(
                x => x.Id == vehicleId && x.TenantId == tenant.Id && x.Status == VehicleStatus.Available,
                cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");

        var item = MapVehicleListItem(vehicle);
        return new PublicVehicleDetail
        {
            Id = item.Id,
            Brand = item.Brand,
            Model = item.Model,
            Year = item.Year,
            PlateNumber = item.PlateNumber,
            CategoryName = item.CategoryName,
            DailyPrice = item.DailyPrice,
            Status = item.Status,
            ImageUrl = item.ImageUrl,
            ImageUrls = vehicle.Images
                .OrderBy(x => x.SortOrder)
                .Select(x => x.Url)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<PublicLocationItem>> ListLocationsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(slug, cancellationToken);

        return await db.Locations
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new PublicLocationItem
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                City = x.City,
                Type = x.Type.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicAvailabilityResponse> CheckAvailabilityAsync(
        string slug,
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(slug, cancellationToken);
        var currency = await GetCurrencyAsync(tenant.Id, cancellationToken);

        try
        {
            NormalizeRange(startAt, endAt, out startAt, out endAt);
        }
        catch (ValidationException ex)
        {
            return new PublicAvailabilityResponse
            {
                IsAvailable = false,
                Reason = ex.Message,
                Currency = currency
            };
        }

        var vehicle = await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == vehicleId && x.TenantId == tenant.Id && x.Status == VehicleStatus.Available,
                cancellationToken);

        if (vehicle is null)
        {
            return new PublicAvailabilityResponse
            {
                IsAvailable = false,
                Reason = "Vehicle not found.",
                Currency = currency
            };
        }

        if (await HasOverlapAsync(tenant.Id, vehicleId, startAt, endAt, cancellationToken))
        {
            return new PublicAvailabilityResponse
            {
                IsAvailable = false,
                Reason = "Vehicle is not available for the selected dates.",
                Currency = currency
            };
        }

        var billableDays = CalculateBillableDays(startAt, endAt);
        return new PublicAvailabilityResponse
        {
            IsAvailable = true,
            BillableDays = billableDays,
            EstimatedTotalPrice = vehicle.DailyPrice * billableDays,
            Currency = currency
        };
    }

    public async Task<PublicReservationResponse> CreateReservationAsync(
        string slug,
        PublicCreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(slug, cancellationToken);
        NormalizeRange(request.StartAt, request.EndAt, out var startAt, out var endAt);

        if (string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ValidationException("Customer name, email and phone are required.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var vehicle = await db.Vehicles
            .FirstOrDefaultAsync(
                x => x.Id == request.VehicleId && x.TenantId == tenant.Id && x.Status == VehicleStatus.Available,
                cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");

        await EnsurePickupLocationAsync(tenant.Id, request.PickupLocationId, cancellationToken);
        await EnsureDropoffLocationAsync(tenant.Id, request.DropoffLocationId, cancellationToken);

        if (await HasOverlapAsync(tenant.Id, vehicle.Id, startAt, endAt, cancellationToken))
        {
            throw new BusinessException("Vehicle already has an overlapping reservation.");
        }

        var customer = await UpsertCustomerAsync(tenant.Id, request, cancellationToken);
        var billableDays = CalculateBillableDays(startAt, endAt);
        var currency = await GetCurrencyAsync(tenant.Id, cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            VehicleId = vehicle.Id,
            CustomerId = customer.Id,
            PickupLocationId = request.PickupLocationId,
            DropoffLocationId = request.DropoffLocationId,
            StartAt = startAt,
            EndAt = endAt,
            Status = ReservationStatus.Pending,
            DailyPriceSnapshot = vehicle.DailyPrice,
            TotalPrice = vehicle.DailyPrice * billableDays,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PublicReservationResponse
        {
            Id = reservation.Id,
            Status = reservation.Status.ToString(),
            TotalPrice = reservation.TotalPrice,
            Currency = currency,
            StartAt = reservation.StartAt,
            EndAt = reservation.EndAt,
            VehicleLabel = $"{vehicle.Brand} {vehicle.Model}"
        };
    }

    private async Task<Tenant> ResolveTenantAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new NotFoundException("Tenant not found.");
        }

        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.Slug == normalized, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Cancelled)
        {
            throw new NotFoundException("Tenant not found.");
        }

        if (!tenant.WebsiteEnabled)
        {
            throw new NotFoundException("Tenant not found.");
        }

        return tenant;
    }

    private async Task<Customer> UpsertCustomerAsync(
        Guid tenantId,
        PublicCreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await db.Customers
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Email == email,
                cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
            return customer;
        }

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Phone = request.Phone.Trim();
        customer.IsActive = true;
        if (customer.IsDeleted)
        {
            customer.IsDeleted = false;
            customer.DeletedAt = null;
        }

        customer.UpdatedAt = DateTime.UtcNow;
        return customer;
    }

    private async Task EnsurePickupLocationAsync(
        Guid tenantId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var location = await db.Locations.FirstOrDefaultAsync(
            x => x.Id == locationId && x.TenantId == tenantId && x.IsActive,
            cancellationToken)
            ?? throw new ValidationException("Pickup location not found or inactive.");

        if (location.Type is not (LocationType.Branch or LocationType.Pickup))
        {
            throw new ValidationException("Pickup location must be a Branch or Pickup type.");
        }
    }

    private async Task EnsureDropoffLocationAsync(
        Guid tenantId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var location = await db.Locations.FirstOrDefaultAsync(
            x => x.Id == locationId && x.TenantId == tenantId && x.IsActive,
            cancellationToken)
            ?? throw new ValidationException("Dropoff location not found or inactive.");

        if (location.Type is not (LocationType.Branch or LocationType.Dropoff))
        {
            throw new ValidationException("Dropoff location must be a Branch or Dropoff type.");
        }
    }

    private async Task<bool> HasOverlapAsync(
        Guid tenantId,
        Guid vehicleId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        return await db.Reservations.AnyAsync(
            x =>
                x.TenantId == tenantId
                && x.VehicleId == vehicleId
                && ActiveStatuses.Contains(x.Status)
                && x.StartAt < endAt
                && x.EndAt > startAt,
            cancellationToken);
    }

    private async Task<string> GetCurrencyAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await db.TenantSettings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "TRY";
    }

    private static void NormalizeRange(DateTime startAt, DateTime endAt, out DateTime start, out DateTime end)
    {
        start = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        end = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);
        if (end <= start)
        {
            throw new ValidationException("End must be after start.");
        }
    }

    private static int CalculateBillableDays(DateTime startAt, DateTime endAt)
    {
        var days = (int)Math.Ceiling((endAt - startAt).TotalDays);
        return Math.Max(1, days);
    }

    private static PublicTenantResponse MapTenant(Tenant tenant) => new()
    {
        Name = tenant.Name,
        Slug = tenant.Slug,
        LogoUrl = tenant.LogoUrl,
        Description = tenant.Description,
        PrimaryColor = tenant.PrimaryColor,
        AccentColor = tenant.AccentColor,
        Phone = tenant.Phone,
        Address = tenant.Address,
        Currency = tenant.Settings?.Currency ?? "TRY"
    };

    private static PublicVehicleListItem MapVehicleListItem(Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Year = vehicle.Year,
        PlateNumber = vehicle.PlateNumber,
        CategoryName = vehicle.Category?.Name,
        DailyPrice = vehicle.DailyPrice,
        Status = vehicle.Status.ToString(),
        ImageUrl = vehicle.Images.OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault()
    };

    public static bool IsValidHexColor(string? color) =>
        !string.IsNullOrWhiteSpace(color) && HexColor.IsMatch(color.Trim());
}
