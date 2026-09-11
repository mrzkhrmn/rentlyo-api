using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rentlyo.Application.Interfaces;
using Rentlyo.Infrastructure.Auth;
using Rentlyo.Infrastructure.Customers;
using Rentlyo.Infrastructure.Dashboard;
using Rentlyo.Infrastructure.Hangfire;
using Rentlyo.Infrastructure.Locations;
using Rentlyo.Infrastructure.Payments;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Infrastructure.Platform;
using Rentlyo.Infrastructure.Public;
using Rentlyo.Infrastructure.Redis;
using Rentlyo.Infrastructure.Reservations;
using Rentlyo.Infrastructure.Subscriptions;
using Rentlyo.Infrastructure.Tenancy;
using Rentlyo.Infrastructure.Vehicles;
using StackExchange.Redis;

namespace Rentlyo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true
            }));
        services.AddHangfireServer();

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                RedisConnectionFactory.Create(redisConnection));
        }

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IVehicleCategoryService, VehicleCategoryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPaymentGateway, StubPaymentGateway>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISubscriptionBillingGateway, StubSubscriptionBillingGateway>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPublicCatalogService, PublicCatalogService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<SubscriptionChecksJob>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();

        return services;
    }
}
