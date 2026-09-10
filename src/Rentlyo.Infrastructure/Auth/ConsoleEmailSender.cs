using Microsoft.Extensions.Logging;
using Rentlyo.Application.Interfaces;

namespace Rentlyo.Infrastructure.Auth;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email stub → To: {To} | Subject: {Subject} | Body: {Body}",
            to,
            subject,
            body);

        return Task.CompletedTask;
    }
}
