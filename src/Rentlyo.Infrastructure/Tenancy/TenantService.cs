using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.Infrastructure.Tenancy;

public class TenantService(
    ApplicationDbContext db,
    ITenantContext tenantContext) : ITenantService
{
    public async Task<TenantResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var tenant = await LoadTenantAsync(cancellationToken);
        return MapTenant(tenant);
    }

    public async Task<TenantResponse> UpdateProfileAsync(
        UpdateTenantProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManageSettings();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Company name is required.");
        }

        var tenant = await db.Tenants
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == tenantContext.TenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.Name = request.Name.Trim();
        tenant.Phone = NormalizeOptional(request.Phone);
        tenant.Address = NormalizeOptional(request.Address);
        tenant.TaxNumber = NormalizeOptional(request.TaxNumber);
        tenant.LogoUrl = NormalizeOptional(request.LogoUrl);

        await db.SaveChangesAsync(cancellationToken);
        return MapTenant(tenant);
    }

    public async Task<TenantSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var settings = await EnsureSettingsAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<TenantSettingsResponse> UpdateSettingsAsync(
        UpdateTenantSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManageSettings();

        if (string.IsNullOrWhiteSpace(request.Timezone)
            || string.IsNullOrWhiteSpace(request.Currency)
            || string.IsNullOrWhiteSpace(request.Locale))
        {
            throw new ValidationException("Timezone, currency and locale are required.");
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        settings.Timezone = request.Timezone.Trim();
        settings.Currency = request.Currency.Trim().ToUpperInvariant();
        settings.Locale = request.Locale.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<IReadOnlyList<PlanResponse>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        return await db.SubscriptionPlans
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.MaxVehicles)
            .Select(x => new PlanResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                MaxVehicles = x.MaxVehicles,
                MaxUsers = x.MaxUsers
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantResponse> ChangePlanAsync(
        ChangePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManageSettings();

        var plan = await db.SubscriptionPlans
            .FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Plan not found.");

        var tenant = await db.Tenants
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == tenantContext.TenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.PlanId = plan.Id;
        tenant.Plan = plan;
        await db.SaveChangesAsync(cancellationToken);
        return MapTenant(tenant);
    }

    private async Task<Tenant> LoadTenantAsync(CancellationToken cancellationToken)
    {
        return await db.Tenants
            .AsNoTracking()
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == tenantContext.TenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");
    }

    private async Task<TenantSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantContext.TenantId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new TenantSettings
        {
            TenantId = tenantContext.TenantId,
            Timezone = "Europe/Istanbul",
            Currency = "TRY",
            Locale = "tr-TR"
        };

        db.TenantSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private void EnsureAuthenticated()
    {
        if (!tenantContext.IsAuthenticated)
        {
            throw new UnauthorizedAppException("Tenant context is required.");
        }
    }

    private void EnsureCanManageSettings()
    {
        EnsureAuthenticated();

        if (tenantContext.Role is not (SystemRoles.Owner or SystemRoles.Admin))
        {
            throw new ForbiddenException("You do not have permission to manage tenant settings.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TenantResponse MapTenant(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Slug = tenant.Slug,
        Status = tenant.Status.ToString(),
        Phone = tenant.Phone,
        Address = tenant.Address,
        TaxNumber = tenant.TaxNumber,
        LogoUrl = tenant.LogoUrl,
        CreatedAt = tenant.CreatedAt,
        Plan = new PlanResponse
        {
            Id = tenant.Plan.Id,
            Code = tenant.Plan.Code,
            Name = tenant.Plan.Name,
            MaxVehicles = tenant.Plan.MaxVehicles,
            MaxUsers = tenant.Plan.MaxUsers
        }
    };

    private static TenantSettingsResponse MapSettings(TenantSettings settings) => new()
    {
        Timezone = settings.Timezone,
        Currency = settings.Currency,
        Locale = settings.Locale
    };
}
