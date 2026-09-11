namespace Rentlyo.Infrastructure.Hangfire;

/// <summary>
/// Hangfire recurring jobs and background processing for SaaS subscription lifecycle.
/// </summary>
public static class HangfireMarker
{
    public static readonly string SubscriptionChecksJobId = HangfireJobs.SubscriptionChecks;
}
