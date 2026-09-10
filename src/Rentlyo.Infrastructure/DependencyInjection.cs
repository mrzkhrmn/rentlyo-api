using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rentlyo.Application.Interfaces;
using Rentlyo.Infrastructure.Auth;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Infrastructure.Redis;
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
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();

        return services;
    }
}
