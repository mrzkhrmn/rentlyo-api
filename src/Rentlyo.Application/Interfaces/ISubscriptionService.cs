using Rentlyo.Application.DTOs.Subscriptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.Application.Interfaces;

public interface ISubscriptionBillingGateway
{
    Task<SubscriptionCheckoutResult> CreateCheckoutAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}

public sealed class SubscriptionCheckoutResult
{
    public string ProviderReference { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
}

public interface ISubscriptionService
{
    Task EnsureSubscriptionForTenantAsync(
        Guid tenantId,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<PaginatedResult<SubscriptionPaymentListItem>> ListPaymentsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ChangeSubscriptionPlanResult> ChangePlanAsync(
        ChangeSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPaymentListItem> StartCheckoutAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> RecordManualPaymentAsync(
        Guid paymentId,
        RecordSubscriptionPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> CancelAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> ResumeAsync(CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(
        SubscriptionWebhookRequest request,
        string? signatureHeader,
        string rawBody,
        CancellationToken cancellationToken = default);

    Task ProcessDueSubscriptionsAsync(CancellationToken cancellationToken = default);
}
