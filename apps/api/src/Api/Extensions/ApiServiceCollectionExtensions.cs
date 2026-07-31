using System.Threading.RateLimiting;
using EnterpriseFramework.Api.ErrorHandling;
using EnterpriseFramework.Api.Options;

namespace EnterpriseFramework.Api.Extensions;

/// <summary>
/// Registration of the API-layer cross-cutting services: ProblemDetails +
/// global exception handling, OpenAPI, CORS, rate limiting and health checks.
/// Kept out of Program.cs so the composition root stays readable.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>Name of the CORS policy applied by <c>UseCors</c>.</summary>
    public const string CorsPolicyName = "Default";

    /// <summary>
    /// Adds every API-layer service to the container.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();

        AddCorsPolicy(services, configuration);
        AddRateLimiting(services, configuration);
        AddHealthChecks(services, configuration);

        return services;
    }

    private static void AddCorsPolicy(IServiceCollection services, IConfiguration configuration)
    {
        var cors =
            configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();

        services.AddCors(options =>
            options.AddPolicy(
                CorsPolicyName,
                policy =>
                    policy
                        .WithOrigins([.. cors.AllowedOrigins])
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
            )
        );
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var options =
            configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Partitioned per client IP: one abusive client cannot exhaust the
            // budget of everyone behind the same instance.
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Realtime connections are exempt. A SignalR client issues a
                // negotiate plus a request per reconnect attempt, and with
                // long-polling one per poll — counting those against the API
                // budget means a flaky network locks the user out of the
                // application itself. SignalR enforces its own connection
                // limits.
                if (context.Request.Path.StartsWithSegments("/hubs"))
                {
                    return RateLimitPartition.GetNoLimiter("hubs");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = 0,
                    }
                );
            });
        });
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        // "ready" checks verify external dependencies; the liveness endpoint
        // (/health/live) uses no checks and only proves the process responds.
        var healthChecks = services.AddHealthChecks();

        var postgres = configuration.GetConnectionString("Postgres");
        if (!string.IsNullOrWhiteSpace(postgres))
        {
            healthChecks.AddNpgSql(postgres, name: "postgres", tags: ["ready"]);
        }

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            healthChecks.AddRedis(redis, name: "redis", tags: ["ready"]);
        }
    }
}
