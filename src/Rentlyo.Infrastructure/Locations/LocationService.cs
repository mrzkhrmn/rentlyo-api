using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Locations;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Locations;

public class LocationService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : ILocationService
{
    public async Task<PaginatedResult<LocationListItem>> ListAsync(
        LocationType? type,
        bool? isActive,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Locations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                x.Address.Contains(term) ||
                (x.City != null && x.City.Contains(term)) ||
                (x.Phone != null && x.Phone.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LocationListItem
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                City = x.City,
                Phone = x.Phone,
                Type = x.Type.ToString(),
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<LocationListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<LocationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        var location = await LoadLocationAsync(id, asTracking: false, cancellationToken);
        return Map(location);
    }

    public async Task<LocationResponse> CreateAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateInput(request.Name, request.Address);
        await EnsureNameAvailableAsync(request.Name.Trim(), excludeId: null, cancellationToken);

        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            City = NormalizeOptional(request.City),
            Phone = NormalizeOptional(request.Phone),
            Notes = NormalizeOptional(request.Notes),
            Type = request.Type,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);
        return Map(location);
    }

    public async Task<LocationResponse> UpdateAsync(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();
        ValidateInput(request.Name, request.Address);

        var location = await LoadLocationAsync(id, asTracking: true, cancellationToken);
        await EnsureNameAvailableAsync(request.Name.Trim(), excludeId: id, cancellationToken);

        location.Name = request.Name.Trim();
        location.Address = request.Address.Trim();
        location.City = NormalizeOptional(request.City);
        location.Phone = NormalizeOptional(request.Phone);
        location.Notes = NormalizeOptional(request.Notes);
        location.Type = request.Type;
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Map(location);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var location = await LoadLocationAsync(id, asTracking: true, cancellationToken);
        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        location.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Location> LoadLocationAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        IQueryable<Location> query = db.Locations
            .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Location not found.");
    }

    private async Task EnsureNameAvailableAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = db.Locations.Where(x =>
            x.TenantId == tenantContext.TenantId &&
            x.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessException("A location with this name already exists.");
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
            throw new ForbiddenException("You do not have permission to manage locations.");
        }
    }

    private static void ValidateInput(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ValidationException("Address is required.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LocationResponse Map(Location location) => new()
    {
        Id = location.Id,
        Name = location.Name,
        Address = location.Address,
        City = location.City,
        Phone = location.Phone,
        Notes = location.Notes,
        Type = location.Type.ToString(),
        IsActive = location.IsActive,
        CreatedAt = location.CreatedAt,
        UpdatedAt = location.UpdatedAt
    };
}
