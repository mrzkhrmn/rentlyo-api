using Rentlyo.Application.DTOs.Payments;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentCheckoutResult> CreateCheckoutAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentCheckoutResult
{
    public string ProviderReference { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
}

public interface IPaymentService
{
    Task<PaginatedResult<PaymentListItem>> ListAsync(
        PaymentStatus? status,
        Guid? reservationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    Task<PaymentResponse> StartCheckoutAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentResponse> RecordManualAsync(
        Guid id,
        RecordPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> RefundAsync(
        Guid id,
        RefundPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(
        PaymentWebhookRequest request,
        string? signatureHeader,
        string rawBody,
        CancellationToken cancellationToken = default);
}
