namespace EnterpriseFramework.Api.Tenancy;

/// <summary>
/// Registers multi-tenancy.
///
/// The services are ALWAYS registered, even when tenancy is disabled: modules
/// depend on the tenant context unconditionally, and a context that reports
/// "not multi-tenant" keeps their code identical in both kinds of deployment.
/// Only the middleware behaviour changes.
/// </summary>
public static class TenancySetup
{
    /// <summary>
    /// Adds the tenancy services to the container.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration (section "Tenancy").</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddTenancy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));

        // Scoped: the tenant belongs to a request, and a singleton would leak
        // one request's tenant into the next.
        services.AddScoped<TenantContext>();
        services.AddScoped<Application.Abstractions.ITenantContext>(provider =>
            provider.GetRequiredService<TenantContext>()
        );

        return services;
    }

    /// <summary>
    /// Adds tenant resolution to the pipeline.
    ///
    /// Must run AFTER authentication: the claim strategy needs a principal,
    /// and a client-supplied tenant is validated against the caller's own.
    /// </summary>
    /// <param name="app">The application pipeline.</param>
    /// <returns>The same application, for chaining.</returns>
    public static IApplicationBuilder UseTenancy(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
