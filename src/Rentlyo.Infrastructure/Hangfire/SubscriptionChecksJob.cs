using Hangfire;
using Rentlyo.Application.Interfaces;

namespace Rentlyo.Infrastructure.Hangfire;

public class SubscriptionChecksJob(ISubscriptionService subscriptionService)
{
    public Task ExecuteAsync() => subscriptionService.ProcessDueSubscriptionsAsync();
}

public static class HangfireJobs
{
    public const string SubscriptionChecks = "subscription-checks";

    public static void RegisterRecurringJobs(IRecurringJobManager recurringJobManager)
    {
        recurringJobManager.AddOrUpdate<SubscriptionChecksJob>(
            SubscriptionChecks,
            job => job.ExecuteAsync(),
            Cron.Daily());
    }
}
