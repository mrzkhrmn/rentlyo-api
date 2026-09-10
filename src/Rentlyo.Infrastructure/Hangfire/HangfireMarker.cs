namespace Rentlyo.Infrastructure.Hangfire;

/// <summary>
/// Hangfire registration will be enabled in later phases when background jobs are introduced.
/// </summary>
public static class HangfireMarker
{
    public const string ReadyForJobs = "Phase later";
}
