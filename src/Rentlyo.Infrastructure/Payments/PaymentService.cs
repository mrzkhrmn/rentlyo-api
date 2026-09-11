using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rentlyo.Application.DTOs.Payments;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Infrastructure.Payments;

public class PaymentService(
    ApplicationDbContext db,
    ITenantContext tenantContext,
    IPaymentGateway paymentGateway,
    IConfiguration configuration) : IPaymentService
{
    public async Task<PaginatedResult<PaymentListItem>> ListAsync(
        PaymentStatus? status,
        Guid? reservationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRead();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Payments
            .AsNoTracking()
            .Include(x => x.Reservation)
            .ThenInclude(x => x.Vehicle)
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (reservationId.HasValue)
        {
            query = query.Where(x => x.ReservationId == reservationId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PaymentListItem>
        {
            Items = items.Select(MapListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanRead();
        var payment = await LoadPaymentAsync(id, asTracking: false, cancellationToken);
        return Map(payment);
    }

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var reservation = await db.Reservations
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(
                x => x.Id == request.ReservationId && x.TenantId == tenantContext.TenantId,
                cancellationToken)
            ?? throw new NotFoundException("Reservation not found.");

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            throw new BusinessException("Cannot create payment for a cancelled reservation.");
        }

        var currency = await db.TenantSettings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId)
            .Select(x => x.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "TRY";

        var alreadyPaid = await db.Payments
            .Where(x =>
                x.TenantId == tenantContext.TenantId
                && x.ReservationId == reservation.Id
                && (x.Status == PaymentStatus.Paid || x.Status == PaymentStatus.PartiallyRefunded))
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var alreadyRefunded = await db.PaymentRefunds
            .Where(x =>
                x.Payment.TenantId == tenantContext.TenantId
                && x.Payment.ReservationId == reservation.Id
                && x.Status == RefundStatus.Completed)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var outstanding = reservation.TotalPrice - (alreadyPaid - alreadyRefunded);
        if (outstanding <= 0)
        {
            throw new BusinessException("Reservation is already fully paid.");
        }

        var amount = request.Amount ?? outstanding;
        if (amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        if (amount > outstanding)
        {
            throw new ValidationException($"Amount cannot exceed outstanding balance ({outstanding}).");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            ReservationId = reservation.Id,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            Method = request.Method ?? PaymentMethod.Other,
            CreatedAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Type = PaymentTransactionType.Created,
            Amount = amount,
            Note = "Payment created",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(payment.Id, cancellationToken);
    }

    public async Task<PaymentResponse> StartCheckoutAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var payment = await LoadPaymentAsync(id, asTracking: true, cancellationToken);
        if (payment.Status != PaymentStatus.Pending)
        {
            throw new BusinessException("Only pending payments can start checkout.");
        }

        var checkout = await paymentGateway.CreateCheckoutAsync(
            payment.Id,
            payment.Amount,
            payment.Currency,
            cancellationToken);

        payment.Method = PaymentMethod.Card;
        payment.ProviderReference = checkout.ProviderReference;
        payment.ExternalCheckoutUrl = checkout.CheckoutUrl;
        payment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<PaymentResponse> RecordManualAsync(
        Guid id,
        RecordPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        var payment = await LoadPaymentAsync(id, asTracking: true, cancellationToken);
        if (payment.Status != PaymentStatus.Pending)
        {
            throw new BusinessException("Only pending payments can be recorded as paid.");
        }

        payment.Status = PaymentStatus.Paid;
        payment.Method = request.Method;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Type = PaymentTransactionType.MarkedPaid,
            Amount = payment.Amount,
            Note = request.Note ?? $"Recorded as {request.Method}",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<PaymentResponse> RefundAsync(
        Guid id,
        RefundPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanWrite();

        if (request.Amount <= 0)
        {
            throw new ValidationException("Refund amount must be greater than zero.");
        }

        var payment = await LoadPaymentAsync(id, asTracking: true, cancellationToken);
        if (payment.Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
        {
            throw new BusinessException("Only paid payments can be refunded.");
        }

        var refundedSoFar = payment.Refunds
            .Where(x => x.Status == RefundStatus.Completed)
            .Sum(x => x.Amount);

        var remaining = payment.Amount - refundedSoFar;
        if (request.Amount > remaining)
        {
            throw new ValidationException($"Refund cannot exceed remaining amount ({remaining}).");
        }

        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Amount = request.Amount,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = RefundStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        db.PaymentRefunds.Add(refund);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Type = PaymentTransactionType.Refunded,
            Amount = request.Amount,
            Note = refund.Reason ?? "Refund completed",
            CreatedAt = DateTime.UtcNow
        });

        var totalRefunded = refundedSoFar + request.Amount;
        payment.Status = totalRefunded >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        payment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task HandleWebhookAsync(
        PaymentWebhookRequest request,
        string? signatureHeader,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        ValidateWebhookSignature(rawBody, signatureHeader);

        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            throw new ValidationException("Provider reference is required.");
        }

        var payment = await db.Payments
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.ProviderReference == request.ProviderReference, cancellationToken)
            ?? throw new NotFoundException("Payment not found for provider reference.");

        if (payment.Status == PaymentStatus.Paid)
        {
            return;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new BusinessException("Payment cannot be marked paid from webhook in current status.");
        }

        if (!string.Equals(request.EventType, "payment.paid", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Type = PaymentTransactionType.Failed,
                Amount = payment.Amount,
                Note = $"Webhook event: {request.EventType}",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Type = PaymentTransactionType.WebhookPaid,
            Amount = payment.Amount,
            Note = "Paid via webhook",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private void ValidateWebhookSignature(string rawBody, string? signatureHeader)
    {
        var secret = configuration["PAYMENT_WEBHOOK_SECRET"]
            ?? configuration["Payment:WebhookSecret"]
            ?? throw new UnauthorizedAppException("Payment webhook secret is not configured.");

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new UnauthorizedAppException("Missing payment webhook signature.");
        }

        var expected = ComputeHmacSha256(secret, rawBody);
        var provided = signatureHeader.Trim().ToLowerInvariant();
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length != providedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            throw new UnauthorizedAppException("Invalid payment webhook signature.");
        }
    }

    private static string ComputeHmacSha256(string secret, string payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<Payment> LoadPaymentAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        IQueryable<Payment> query = db.Payments
            .Include(x => x.Reservation).ThenInclude(x => x.Vehicle)
            .Include(x => x.Refunds)
            .Include(x => x.Transactions)
            .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Payment not found.");
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
            throw new ForbiddenException("You do not have permission to manage payments.");
        }
    }

    private static PaymentListItem MapListItem(Payment payment) => new()
    {
        Id = payment.Id,
        ReservationId = payment.ReservationId,
        ReservationLabel = $"{payment.Reservation.Vehicle.Brand} {payment.Reservation.Vehicle.Model}",
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status.ToString(),
        Method = payment.Method.ToString(),
        CreatedAt = payment.CreatedAt,
        PaidAt = payment.PaidAt
    };

    private static PaymentResponse Map(Payment payment)
    {
        var refunded = payment.Refunds
            .Where(x => x.Status == RefundStatus.Completed)
            .Sum(x => x.Amount);

        return new PaymentResponse
        {
            Id = payment.Id,
            ReservationId = payment.ReservationId,
            ReservationLabel = $"{payment.Reservation.Vehicle.Brand} {payment.Reservation.Vehicle.Model}",
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            Method = payment.Method.ToString(),
            ProviderReference = payment.ProviderReference,
            ExternalCheckoutUrl = payment.ExternalCheckoutUrl,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt,
            RefundedAmount = refunded,
            Refunds = payment.Refunds
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PaymentRefundDto
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    Reason = x.Reason,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToList(),
            Transactions = payment.Transactions
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PaymentTransactionDto
                {
                    Id = x.Id,
                    Type = x.Type.ToString(),
                    Amount = x.Amount,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };
    }
}
