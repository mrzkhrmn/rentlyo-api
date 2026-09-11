using Microsoft.Extensions.Configuration;
using Rentlyo.Application.Interfaces;

namespace Rentlyo.Infrastructure.Payments;

public class StubPaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public Task<PaymentCheckoutResult> CreateCheckoutAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var frontendOrigin = configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var reference = $"stub_{paymentId:N}";

        return Task.FromResult(new PaymentCheckoutResult
        {
            ProviderReference = reference,
            CheckoutUrl = $"{frontendOrigin.TrimEnd('/')}/dashboard/payments/{paymentId}?checkout=stub&ref={reference}&amount={amount}&currency={currency}"
        });
    }
}
