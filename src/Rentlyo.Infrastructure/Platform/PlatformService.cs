using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rentlyo.Application.DTOs.Platform;
using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Platform;

public class PlatformService(
    ApplicationDbContext db,
    IJwtTokenService jwtTokenService,
    IConfiguration configuration) : IPlatformService
{
    private readonly PasswordHasher<PlatformUser> _passwordHasher = new();

    public async Task EnsureSeedAdminAsync(CancellationToken cancellationToken = default)
    {
        if (await db.PlatformUsers.AnyAsync(cancellationToken))
        {
            return;
        }

        var email = (configuration["PLATFORM_ADMIN_EMAIL"] ?? "admin@rentlyo.local").Trim().ToLowerInvariant();
        var password = configuration["PLATFORM_ADMIN_PASSWORD"] ?? "ChangeMe!Platform1";
        var fullName = configuration["PLATFORM_ADMIN_NAME"] ?? "Platform Admin";

        var user = new PlatformUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        db.PlatformUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlatformAuthResponse> LoginAsync(
        PlatformLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.PlatformUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid credentials.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("Platform user is inactive.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        var (token, expiresIn) = jwtTokenService.CreatePlatformAccessToken(user);
        return new PlatformAuthResponse
        {
            AccessToken = token,
            ExpiresIn = expiresIn,
            User = new PlatformUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName
            }
        };
    }

    public async Task<PaginatedResult<PlatformTenantListItem>> ListTenantsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Tenants
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(x => x.Subscription)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term) || x.Slug.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PlatformTenantListItem>
        {
            Items = items.Select(MapListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PlatformTenantDetail> GetTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var ownerEmail = await db.Users
            .AsNoTracking()
            .Where(x => x.TenantId == id)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Email)
            .FirstOrDefaultAsync(cancellationToken);

        var vehicleCount = await db.Vehicles.CountAsync(x => x.TenantId == id, cancellationToken);
        var reservationCount = await db.Reservations.CountAsync(x => x.TenantId == id, cancellationToken);

        var item = MapListItem(tenant);
        return new PlatformTenantDetail
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            Status = item.Status,
            PlanName = item.PlanName,
            SubscriptionStatus = item.SubscriptionStatus,
            WebsiteEnabled = item.WebsiteEnabled,
            CreatedAt = item.CreatedAt,
            Phone = tenant.Phone,
            Address = tenant.Address,
            Email = ownerEmail,
            VehicleCount = vehicleCount,
            ReservationCount = reservationCount
        };
    }

    public async Task SuspendTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");
        tenant.Status = TenantStatus.Suspended;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");
        tenant.Status = TenantStatus.Active;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetWebsiteEnabledAsync(
        Guid id,
        SetWebsiteEnabledRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");
        tenant.WebsiteEnabled = request.WebsiteEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedResult<LeadResponse>> ListLeadsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Leads.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<LeadResponse>
        {
            Items = items.Select(MapLead).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<LeadResponse> UpdateLeadStatusAsync(
        Guid id,
        UpdateLeadStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<LeadStatus>(request.Status, ignoreCase: true, out var status))
        {
            throw new ValidationException("Invalid lead status.");
        }

        var lead = await db.Leads.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lead not found.");

        lead.Status = status;
        lead.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return MapLead(lead);
    }

    private static PlatformTenantListItem MapListItem(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Slug = tenant.Slug,
        Status = tenant.Status.ToString(),
        PlanName = tenant.Plan.Name,
        SubscriptionStatus = tenant.Subscription?.Status.ToString(),
        WebsiteEnabled = tenant.WebsiteEnabled,
        CreatedAt = tenant.CreatedAt
    };

    private static LeadResponse MapLead(Lead lead) => new()
    {
        Id = lead.Id,
        Name = lead.Name,
        Company = lead.Company,
        Email = lead.Email,
        Phone = lead.Phone,
        Message = lead.Message,
        WantsWebsite = lead.WantsWebsite,
        Status = lead.Status.ToString(),
        CreatedAt = lead.CreatedAt,
        UpdatedAt = lead.UpdatedAt
    };
}
