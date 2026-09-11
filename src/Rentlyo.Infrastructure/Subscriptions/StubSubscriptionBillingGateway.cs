using Microsoft.Extensions.Configuration;
using Rentlyo.Application.Interfaces;

namespace Rentlyo.Infrastructure.Subscriptions;

public class StubSubscriptionBillingGateway(IConfiguration configuration) : ISubscriptionBillingGateway
{
    public Task<SubscriptionCheckoutResult> CreateCheckoutAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var frontendOrigin = configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var reference = $"sub_stub_{paymentId:N}";

        return Task.FromResult(new SubscriptionCheckoutResult
        {
            ProviderReference = reference,
            CheckoutUrl =
                $"{frontendOrigin.TrimEnd('/')}/dashboard/billing?checkout=stub&paymentId={paymentId}&ref={reference}&amount={amount}&currency={currency}"
        });
    }
}
