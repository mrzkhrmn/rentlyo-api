using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Vehicles;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Vehicles;

public class VehicleService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : IVehicleService
{
    public async Task<PaginatedResult<VehicleListItem>> ListAsync(
        VehicleStatus? status,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Vehicles
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<VehicleListItem>
        {
            Items = items.Select(MapListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<VehicleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        var vehicle = await LoadVehicleAsync(id, asTracking: false, cancellationToken);
        return MapVehicle(vehicle);
    }

    public async Task<VehicleResponse> CreateAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateVehicleInput(request.Brand, request.Model, request.Year, request.PlateNumber, request.DailyPrice);

        var tenant = await db.Tenants
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == tenantContext.TenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var activeCount = await db.Vehicles
            .CountAsync(x => x.TenantId == tenantContext.TenantId, cancellationToken);

        if (activeCount >= tenant.Plan.MaxVehicles)
        {
            throw new BusinessException(
                $"Vehicle limit reached for plan '{tenant.Plan.Name}' ({tenant.Plan.MaxVehicles}).");
        }

        await EnsureCategoryBelongsToTenantAsync(request.CategoryId, cancellationToken);
        await EnsurePlateAvailableAsync(request.PlateNumber, excludeVehicleId: null, cancellationToken);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            CategoryId = request.CategoryId,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            PlateNumber = NormalizePlate(request.PlateNumber),
            DailyPrice = request.DailyPrice,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleResponse> UpdateAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateVehicleInput(request.Brand, request.Model, request.Year, request.PlateNumber, request.DailyPrice);

        var vehicle = await LoadVehicleAsync(id, asTracking: true, cancellationToken);
        await EnsureCategoryBelongsToTenantAsync(request.CategoryId, cancellationToken);
        await EnsurePlateAvailableAsync(request.PlateNumber, excludeVehicleId: id, cancellationToken);

        vehicle.CategoryId = request.CategoryId;
        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.PlateNumber = NormalizePlate(request.PlateNumber);
        vehicle.DailyPrice = request.DailyPrice;
        vehicle.Status = request.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var vehicle = await LoadVehicleAsync(id, asTracking: true, cancellationToken);
        vehicle.IsDeleted = true;
        vehicle.DeletedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VehicleImageDto> AddImageAsync(
        Guid vehicleId,
        AddVehicleImageRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ValidationException("Image URL is required.");
        }

        if (request.Url.Trim().Length > 1000)
        {
            throw new ValidationException("Image URL is too long.");
        }

        var vehicle = await LoadVehicleAsync(vehicleId, asTracking: true, cancellationToken);

        if (request.IsPrimary)
        {
            foreach (var image in vehicle.Images)
            {
                image.IsPrimary = false;
            }
        }

        var nextOrder = vehicle.Images.Count == 0 ? 0 : vehicle.Images.Max(x => x.SortOrder) + 1;
        var entity = new VehicleImage
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.Id,
            Url = request.Url.Trim(),
            SortOrder = nextOrder,
            IsPrimary = request.IsPrimary || vehicle.Images.Count == 0
        };

        db.VehicleImages.Add(entity);
        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapImage(entity);
    }

    public async Task DeleteImageAsync(
        Guid vehicleId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var vehicle = await LoadVehicleAsync(vehicleId, asTracking: true, cancellationToken);
        var image = vehicle.Images.FirstOrDefault(x => x.Id == imageId)
            ?? throw new NotFoundException("Image not found.");

        var wasPrimary = image.IsPrimary;
        db.VehicleImages.Remove(image);

        if (wasPrimary)
        {
            var nextPrimary = vehicle.Images
                .Where(x => x.Id != imageId)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefault();
            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;
            }
        }

        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Vehicle> LoadVehicleAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        IQueryable<Vehicle> query = db.Vehicles
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");
    }

    private async Task EnsureCategoryBelongsToTenantAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        var exists = await db.VehicleCategories.AnyAsync(
            x => x.Id == categoryId.Value && x.TenantId == tenantContext.TenantId,
            cancellationToken);

        if (!exists)
        {
            throw new ValidationException("Category not found for this tenant.");
        }
    }

    private async Task EnsurePlateAvailableAsync(
        string plateNumber,
        Guid? excludeVehicleId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizePlate(plateNumber);
        var query = db.Vehicles.Where(x =>
            x.TenantId == tenantContext.TenantId &&
            x.PlateNumber == normalized);

        if (excludeVehicleId.HasValue)
        {
            query = query.Where(x => x.Id != excludeVehicleId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessException("A vehicle with this plate number already exists.");
        }
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
            throw new ForbiddenException("You do not have permission to manage vehicles.");
        }
    }

    private static void ValidateVehicleInput(
        string brand,
        string model,
        int year,
        string plateNumber,
        decimal dailyPrice)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ValidationException("Brand is required.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ValidationException("Model is required.");
        }

        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            throw new ValidationException("Plate number is required.");
        }

        if (year < 1980 || year > DateTime.UtcNow.Year + 1)
        {
            throw new ValidationException("Year is invalid.");
        }

        if (dailyPrice < 0)
        {
            throw new ValidationException("Daily price cannot be negative.");
        }
    }

    private static string NormalizePlate(string plateNumber) =>
        plateNumber.Trim().ToUpperInvariant();

    private static VehicleListItem MapListItem(Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Year = vehicle.Year,
        PlateNumber = vehicle.PlateNumber,
        DailyPrice = vehicle.DailyPrice,
        Status = vehicle.Status.ToString(),
        CategoryId = vehicle.CategoryId,
        CategoryName = vehicle.Category?.Name,
        PrimaryImageUrl = vehicle.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => x.Url)
            .FirstOrDefault()
    };

    private static VehicleResponse MapVehicle(Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        CategoryId = vehicle.CategoryId,
        CategoryName = vehicle.Category?.Name,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Year = vehicle.Year,
        PlateNumber = vehicle.PlateNumber,
        DailyPrice = vehicle.DailyPrice,
        Status = vehicle.Status.ToString(),
        CreatedAt = vehicle.CreatedAt,
        UpdatedAt = vehicle.UpdatedAt,
        Images = vehicle.Images
            .OrderBy(x => x.SortOrder)
            .Select(MapImage)
            .ToList()
    };

    private static VehicleImageDto MapImage(VehicleImage image) => new()
    {
        Id = image.Id,
        Url = image.Url,
        SortOrder = image.SortOrder,
        IsPrimary = image.IsPrimary
    };
}
