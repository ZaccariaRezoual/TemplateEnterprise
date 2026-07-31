using EnterpriseFramework.Application.Abstractions;

namespace EnterpriseFramework.Api.Features;

/// <summary>
/// Exposes the feature flags evaluated for the caller.
///
/// Host-level rather than a module: flags are infrastructure every module may
/// read, and a module owning them would make the whole application depend on
/// it being installed.
/// </summary>
public static class FeaturesEndpoints
{
    /// <summary>
    /// Adds the feature-flag services.
    ///
    /// Always registered: features call <see cref="IFeatureFlags"/>
    /// unconditionally, and a deployment that declares no flags simply
    /// answers "off" to everything.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration (section "Features").</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<FeatureFlagOptions>(
            configuration.GetSection(FeatureFlagOptions.SectionName)
        );
        // Scoped: evaluation depends on the caller and their tenant.
        services.AddScoped<IFeatureFlags, ConfigurationFeatureFlags>();

        return services;
    }

    /// <summary>
    /// Maps the feature endpoints.
    /// </summary>
    /// <param name="endpoints">Host endpoint route builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Anonymous: the client fetches flags before signing in, since a flag
        // may govern whether the sign-in screen even offers something. Flags
        // are not secrets — they are switches, and permissions remain the
        // security boundary.
        endpoints
            .MapGet(
                "/api/features",
                async (IFeatureFlags flags, CancellationToken ct) =>
                    TypedResults.Ok(await flags.GetAllAsync(ct))
            )
            .WithName("featuresGetAll")
            .WithSummary("Returns every feature flag evaluated for the caller.")
            .WithTags("Features")
            .AllowAnonymous();

        return endpoints;
    }
}
