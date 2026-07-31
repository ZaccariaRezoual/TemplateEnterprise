using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Infrastructure.Caching;
using EnterpriseFramework.Infrastructure.Events;
using EnterpriseFramework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Infrastructure;

/// <summary>
/// Composition entry point of the Infrastructure layer.
///
/// Registers the EF Core context (PostgreSQL, connection string
/// "ConnectionStrings:Postgres") and the in-process event bus.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Infrastructure services to the container.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration (connection strings).</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown at startup when the "Postgres" connection string is missing:
    /// failing fast beats a broken runtime later.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IEventBus, MediatREventBus>();

        AddCaching(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers the cache: Redis when configured, process memory otherwise.
    ///
    /// The in-memory fallback keeps development free of a Redis dependency,
    /// but it is per-replica — an invalidation on one instance leaves the
    /// others stale — so a scaled deployment must configure Redis.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration.</param>
    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "enterprise:";
            });
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();
    }
}
