using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rentlyo.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        const int maxAttempts = 15;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully");
                return;
            }
            catch (SqlException ex) when (ex.Number == 1801)
            {
                // Race: EF tried CREATE DATABASE while SQL Server already has it.
                logger.LogWarning(
                    "Database already exists (attempt {Attempt}/{Max}). Retrying migrate...",
                    attempt,
                    maxAttempts);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Database migration attempt {Attempt}/{Max} failed. Retrying...",
                    attempt,
                    maxAttempts);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Final attempt — let it throw if it still fails.
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully");
    }
}
