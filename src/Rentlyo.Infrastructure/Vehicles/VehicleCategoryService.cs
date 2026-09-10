using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Vehicles;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.Infrastructure.Vehicles;

public class VehicleCategoryService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : IVehicleCategoryService
{
    public async Task<IReadOnlyList<VehicleCategoryResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        return await db.VehicleCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId)
            .OrderBy(x => x.Name)
            .Select(x => new VehicleCategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                VehicleCount = x.Vehicles.Count(v => !v.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VehicleCategoryResponse> CreateAsync(
        CreateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateName(request.Name);

        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(name, excludeId: null, cancellationToken);

        var category = new VehicleCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = name,
            Description = NormalizeOptional(request.Description),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        db.VehicleCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return Map(category, vehicleCount: 0);
    }

    public async Task<VehicleCategoryResponse> UpdateAsync(
        Guid id,
        UpdateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateName(request.Name);

        var category = await db.VehicleCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(name, excludeId: id, cancellationToken);

        category.Name = name;
        category.Description = NormalizeOptional(request.Description);
        category.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        var vehicleCount = await db.Vehicles.CountAsync(
            x => x.CategoryId == id && x.TenantId == tenantContext.TenantId,
            cancellationToken);

        return Map(category, vehicleCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var category = await db.VehicleCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        var hasVehicles = await db.Vehicles.AnyAsync(
            x => x.CategoryId == id && x.TenantId == tenantContext.TenantId,
            cancellationToken);

        if (hasVehicles)
        {
            throw new BusinessException("Cannot delete a category that still has vehicles.");
        }

        db.VehicleCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameAvailableAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = db.VehicleCategories.Where(x =>
            x.TenantId == tenantContext.TenantId &&
            x.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessException("A category with this name already exists.");
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
            throw new ForbiddenException("You do not have permission to manage vehicle categories.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Category name is required.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VehicleCategoryResponse Map(VehicleCategory category, int vehicleCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        VehicleCount = vehicleCount
    };
}
