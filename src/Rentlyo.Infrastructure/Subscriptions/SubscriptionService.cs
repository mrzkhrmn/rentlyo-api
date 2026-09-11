using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rentlyo.Application.DTOs.Subscriptions;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Subscriptions;

public class SubscriptionService(
    ApplicationDbContext db,
    ITenantContext tenantContext,
    ISubscriptionBillingGateway billingGateway,
    IConfiguration configuration) : ISubscriptionService
{
    private static readonly TimeSpan TrialDuration = TimeSpan.FromDays(14);
    private static readonly TimeSpan BillingPeriod = TimeSpan.FromDays(30);
    private static readonly TimeSpan PastDueGrace = TimeSpan.FromDays(7);

    public async Task EnsureSubscriptionForTenantAsync(
        Guid tenantId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        if (await db.Subscriptions.AnyAsync(x => x.TenantId == tenantId, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Trialing,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.Add(TrialDuration),
            TrialEndsAt = now.Add(TrialDuration),
            CancelAtPeriodEnd = false,
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubscriptionResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        var subscription = await LoadSubscriptionAsync(asTracking: false, cancellationToken);
        return await MapAsync(subscription, cancellationToken);
    }

    public async Task<PaginatedResult<SubscriptionPaymentListItem>> ListPaymentsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.SubscriptionPayments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<SubscriptionPaymentListItem>
        {
            Items = items.Select(MapPayment).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ChangeSubscriptionPlanResult> ChangePlanAsync(
        ChangeSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var plan = await db.SubscriptionPlans
            .FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Plan not found.");

        var subscription = await LoadSubscriptionAsync(asTracking: true, cancellationToken);
        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            throw new BusinessException("Cannot change plan on a cancelled or expired subscription. Contact support.");
        }

        if (subscription.PlanId == plan.Id && subscription.PendingPlanId is null)
        {
            throw new BusinessException("Already on this plan.");
        }

        var now = DateTime.UtcNow;

        if (plan.MonthlyPrice <= 0)
        {
            subscription.PlanId = plan.Id;
            subscription.PendingPlanId = null;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStart = now;
            subscription.CurrentPeriodEnd = now.Add(BillingPeriod);
            subscription.TrialEndsAt = null;
            subscription.CancelAtPeriodEnd = false;
            subscription.CancelledAt = null;
            subscription.UpdatedAt = now;

            var tenant = await db.Tenants.FirstAsync(x => x.Id == subscription.TenantId, cancellationToken);
            tenant.PlanId = plan.Id;
            if (tenant.Status == TenantStatus.Suspended)
            {
                tenant.Status = TenantStatus.Active;
            }

            await db.SaveChangesAsync(cancellationToken);
            var mapped = await MapAsync(await ReloadAsync(subscription.Id, cancellationToken), cancellationToken);
            return new ChangeSubscriptionPlanResult
            {
                Subscription = mapped,
                RequiresPayment = false
            };
        }

        var existingPending = await db.SubscriptionPayments
            .FirstOrDefaultAsync(
                x => x.SubscriptionId == subscription.Id && x.Status == SubscriptionPaymentStatus.Pending,
                cancellationToken);

        if (existingPending is not null)
        {
            existingPending.Status = SubscriptionPaymentStatus.Failed;
        }

        subscription.PendingPlanId = plan.Id;
        subscription.UpdatedAt = now;

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            TenantId = subscription.TenantId,
            SubscriptionId = subscription.Id,
            Amount = plan.MonthlyPrice,
            Currency = plan.Currency,
            Status = SubscriptionPaymentStatus.Pending,
            CreatedAt = now
        };

        db.SubscriptionPayments.Add(payment);

        var checkout = await billingGateway.CreateCheckoutAsync(
            payment.Id,
            payment.Amount,
            payment.Currency,
            cancellationToken);
        payment.ProviderReference = checkout.ProviderReference;
        payment.ExternalCheckoutUrl = checkout.CheckoutUrl;

        await db.SaveChangesAsync(cancellationToken);

        return new ChangeSubscriptionPlanResult
        {
            Subscription = await MapAsync(await ReloadAsync(subscription.Id, cancellationToken), cancellationToken),
            Payment = MapPayment(payment),
            RequiresPayment = true
        };
    }

    public async Task<SubscriptionPaymentListItem> StartCheckoutAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var payment = await db.SubscriptionPayments
            .FirstOrDefaultAsync(
                x => x.Id == paymentId && x.TenantId == tenantContext.TenantId,
                cancellationToken)
            ?? throw new NotFoundException("Subscription payment not found.");

        if (payment.Status != SubscriptionPaymentStatus.Pending)
        {
            throw new BusinessException("Only pending payments can start checkout.");
        }

        var checkout = await billingGateway.CreateCheckoutAsync(
            payment.Id,
            payment.Amount,
            payment.Currency,
            cancellationToken);

        payment.ProviderReference = checkout.ProviderReference;
        payment.ExternalCheckoutUrl = checkout.CheckoutUrl;
        await db.SaveChangesAsync(cancellationToken);

        return MapPayment(payment);
    }

    public async Task<SubscriptionResponse> RecordManualPaymentAsync(
        Guid paymentId,
        RecordSubscriptionPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var payment = await db.SubscriptionPayments
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(
                x => x.Id == paymentId && x.TenantId == tenantContext.TenantId,
                cancellationToken)
            ?? throw new NotFoundException("Subscription payment not found.");

        if (payment.Status != SubscriptionPaymentStatus.Pending)
        {
            throw new BusinessException("Only pending payments can be recorded.");
        }

        await MarkPaymentPaidAsync(payment, cancellationToken);
        return await MapAsync(await ReloadAsync(payment.SubscriptionId, cancellationToken), cancellationToken);
    }

    public async Task<SubscriptionResponse> CancelAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var subscription = await LoadSubscriptionAsync(asTracking: true, cancellationToken);
        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            throw new BusinessException("Subscription is already cancelled or expired.");
        }

        subscription.CancelAtPeriodEnd = true;
        subscription.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await MapAsync(await ReloadAsync(subscription.Id, cancellationToken), cancellationToken);
    }

    public async Task<SubscriptionResponse> ResumeAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var subscription = await LoadSubscriptionAsync(asTracking: true, cancellationToken);
        if (!subscription.CancelAtPeriodEnd)
        {
            throw new BusinessException("Subscription is not scheduled for cancellation.");
        }

        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            throw new BusinessException("Cannot resume a cancelled or expired subscription.");
        }

        subscription.CancelAtPeriodEnd = false;
        subscription.CancelledAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await MapAsync(await ReloadAsync(subscription.Id, cancellationToken), cancellationToken);
    }

    public async Task HandleWebhookAsync(
        SubscriptionWebhookRequest request,
        string? signatureHeader,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        ValidateWebhookSignature(rawBody, signatureHeader);

        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            throw new ValidationException("Provider reference is required.");
        }

        var payment = await db.SubscriptionPayments
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.ProviderReference == request.ProviderReference, cancellationToken)
            ?? throw new NotFoundException("Subscription payment not found for provider reference.");

        if (payment.Status == SubscriptionPaymentStatus.Paid)
        {
            return;
        }

        if (payment.Status != SubscriptionPaymentStatus.Pending)
        {
            throw new BusinessException("Payment cannot be marked paid from webhook in current status.");
        }

        if (!string.Equals(request.EventType, "subscription.paid", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = SubscriptionPaymentStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        await MarkPaymentPaidAsync(payment, cancellationToken);
    }

    public async Task ProcessDueSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var subscriptions = await db.Subscriptions
            .Include(x => x.Plan)
            .Include(x => x.Tenant)
            .Where(x =>
                x.Status == SubscriptionStatus.Trialing
                || x.Status == SubscriptionStatus.Active
                || x.Status == SubscriptionStatus.PastDue)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            if (subscription.Status == SubscriptionStatus.Trialing
                && subscription.TrialEndsAt is not null
                && subscription.TrialEndsAt <= now)
            {
                if (subscription.Plan.MonthlyPrice <= 0)
                {
                    subscription.Status = SubscriptionStatus.Active;
                    subscription.CurrentPeriodStart = now;
                    subscription.CurrentPeriodEnd = now.Add(BillingPeriod);
                    subscription.TrialEndsAt = null;
                    subscription.UpdatedAt = now;
                }
                else
                {
                    await MoveToPastDueAsync(subscription, now, cancellationToken);
                }

                continue;
            }

            if (subscription.Status == SubscriptionStatus.Active && subscription.CurrentPeriodEnd <= now)
            {
                if (subscription.CancelAtPeriodEnd)
                {
                    subscription.Status = SubscriptionStatus.Cancelled;
                    subscription.CancelledAt = now;
                    subscription.CancelAtPeriodEnd = false;
                    subscription.UpdatedAt = now;
                    subscription.Tenant.Status = TenantStatus.Suspended;
                    continue;
                }

                if (subscription.Plan.MonthlyPrice <= 0)
                {
                    subscription.CurrentPeriodStart = now;
                    subscription.CurrentPeriodEnd = now.Add(BillingPeriod);
                    subscription.UpdatedAt = now;
                    continue;
                }

                await MoveToPastDueAsync(subscription, now, cancellationToken);
                continue;
            }

            if (subscription.Status == SubscriptionStatus.PastDue
                && subscription.CurrentPeriodEnd.Add(PastDueGrace) <= now)
            {
                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = now;
                subscription.Tenant.Status = TenantStatus.Suspended;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MoveToPastDueAsync(
        Subscription subscription,
        DateTime now,
        CancellationToken cancellationToken)
    {
        subscription.Status = SubscriptionStatus.PastDue;
        subscription.UpdatedAt = now;

        var hasPending = await db.SubscriptionPayments.AnyAsync(
            x => x.SubscriptionId == subscription.Id && x.Status == SubscriptionPaymentStatus.Pending,
            cancellationToken);

        if (hasPending)
        {
            return;
        }

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            TenantId = subscription.TenantId,
            SubscriptionId = subscription.Id,
            Amount = subscription.Plan.MonthlyPrice,
            Currency = subscription.Plan.Currency,
            Status = SubscriptionPaymentStatus.Pending,
            CreatedAt = now
        };

        var checkout = await billingGateway.CreateCheckoutAsync(
            payment.Id,
            payment.Amount,
            payment.Currency,
            cancellationToken);
        payment.ProviderReference = checkout.ProviderReference;
        payment.ExternalCheckoutUrl = checkout.CheckoutUrl;

        db.SubscriptionPayments.Add(payment);
    }

    private async Task MarkPaymentPaidAsync(SubscriptionPayment payment, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .Include(x => x.Tenant)
            .FirstAsync(x => x.Id == payment.SubscriptionId, cancellationToken);

        var now = DateTime.UtcNow;
        payment.Status = SubscriptionPaymentStatus.Paid;
        payment.PaidAt = now;

        var targetPlanId = subscription.PendingPlanId ?? subscription.PlanId;
        var plan = await db.SubscriptionPlans.FirstAsync(x => x.Id == targetPlanId, cancellationToken);

        subscription.PlanId = plan.Id;
        subscription.PendingPlanId = null;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = now.Add(BillingPeriod);
        subscription.TrialEndsAt = null;
        subscription.CancelAtPeriodEnd = false;
        subscription.CancelledAt = null;
        subscription.UpdatedAt = now;

        subscription.Tenant.PlanId = plan.Id;
        if (subscription.Tenant.Status == TenantStatus.Suspended)
        {
            subscription.Tenant.Status = TenantStatus.Active;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private void ValidateWebhookSignature(string rawBody, string? signatureHeader)
    {
        var secret = configuration["SUBSCRIPTION_WEBHOOK_SECRET"]
            ?? configuration["Subscription:WebhookSecret"]
            ?? throw new UnauthorizedAppException("Subscription webhook secret is not configured.");

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new UnauthorizedAppException("Missing subscription webhook signature.");
        }

        var expected = ComputeHmacSha256(secret, rawBody);
        var provided = signatureHeader.Trim().ToLowerInvariant();
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length != providedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            throw new UnauthorizedAppException("Invalid subscription webhook signature.");
        }
    }

    private static string ComputeHmacSha256(string secret, string payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<Subscription> LoadSubscriptionAsync(bool asTracking, CancellationToken cancellationToken)
    {
        IQueryable<Subscription> query = db.Subscriptions
            .Include(x => x.Plan)
            .Include(x => x.PendingPlan)
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        var subscription = await query.FirstOrDefaultAsync(cancellationToken);
        if (subscription is not null)
        {
            return subscription;
        }

        await EnsureSubscriptionForTenantAsync(tenantContext.TenantId, await GetTenantPlanIdAsync(cancellationToken), cancellationToken);
        return await LoadSubscriptionAsync(asTracking, cancellationToken);
    }

    private async Task<Guid> GetTenantPlanIdAsync(CancellationToken cancellationToken)
    {
        return await db.Tenants
            .Where(x => x.Id == tenantContext.TenantId)
            .Select(x => x.PlanId)
            .FirstAsync(cancellationToken);
    }

    private async Task<Subscription> ReloadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Subscriptions
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(x => x.PendingPlan)
            .FirstAsync(x => x.Id == id, cancellationToken);
    }

    private async Task<SubscriptionResponse> MapAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        var pendingPayment = await db.SubscriptionPayments
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscription.Id && x.Status == SubscriptionPaymentStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new SubscriptionResponse
        {
            Id = subscription.Id,
            PlanId = subscription.PlanId,
            Plan = MapPlan(subscription.Plan),
            Status = subscription.Status.ToString(),
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            TrialEndsAt = subscription.TrialEndsAt,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CancelledAt = subscription.CancelledAt,
            PendingPlanId = subscription.PendingPlanId,
            PendingPlan = subscription.PendingPlan is null ? null : MapPlan(subscription.PendingPlan),
            LatestPendingPayment = pendingPayment is null ? null : MapPayment(pendingPayment),
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    private static PlanResponse MapPlan(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Code = plan.Code,
        Name = plan.Name,
        MaxVehicles = plan.MaxVehicles,
        MaxUsers = plan.MaxUsers,
        MonthlyPrice = plan.MonthlyPrice,
        Currency = plan.Currency
    };

    private static SubscriptionPaymentListItem MapPayment(SubscriptionPayment payment) => new()
    {
        Id = payment.Id,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status.ToString(),
        ProviderReference = payment.ProviderReference,
        ExternalCheckoutUrl = payment.ExternalCheckoutUrl,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };

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
        if (tenantContext.Role is not (SystemRoles.Owner or SystemRoles.Admin))
        {
            throw new ForbiddenException("You do not have permission to manage billing.");
        }
    }
}
