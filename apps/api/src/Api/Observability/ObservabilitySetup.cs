using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EnterpriseFramework.Api.Observability;

/// <summary>
/// Wires OpenTelemetry traces and metrics.
///
/// Complements Serilog rather than replacing it: logs say WHAT happened,
/// traces say where the time went across services. Both carry the same
/// correlation id, so one identifier moves between Seq and the tracing
/// backend.
///
/// Does nothing when disabled, which is the default.
/// </summary>
public static class ObservabilitySetup
{
    /// <summary>
    /// Adds tracing and metrics when "Observability:Enabled" is true.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options =
            configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        services.Configure<ObservabilityOptions>(
            configuration.GetSection(ObservabilityOptions.SectionName)
        );

        if (!options.Enabled)
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(options.SamplingRatio))
                    .AddAspNetCoreInstrumentation(instrumentation =>
                        // Health probes fire constantly and carry no
                        // diagnostic value; tracing them buries real traffic.
                        instrumentation.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health")
                    )
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(exporter =>
                        exporter.Endpoint = new Uri(options.OtlpEndpoint)
                    )
            )
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporter =>
                        exporter.Endpoint = new Uri(options.OtlpEndpoint)
                    )
            );

        return services;
    }
}
