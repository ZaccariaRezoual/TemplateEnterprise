using EnterpriseFramework.Application.Abstractions;
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

        return services;
    }
}
