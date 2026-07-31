using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Realtime.Abstractions;
using EnterpriseFramework.Modules.Realtime.Hubs;
using EnterpriseFramework.Modules.Realtime.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EnterpriseFramework.Modules.Realtime;

/// <summary>
/// Realtime module: delivers domain events to connected clients over SignalR.
///
/// It knows no other module. Anything implementing <see cref="IRealtimeEvent"/>
/// is picked up by the open-generic bridge, so a feature becomes live by
/// implementing an interface declared in Application — never by depending on
/// this project or on SignalR.
///
/// Scale-out: when a Redis connection string is configured the SignalR
/// backplane and the connection registry both use it, and multiple API
/// replicas behave as one. Without Redis the module still works, but only for
/// a single instance.
/// </summary>
public sealed class RealtimeModule : IModule
{
    /// <summary>Path the SignalR hub is served on.</summary>
    public const string HubPath = "/hubs/realtime";

    /// <inheritdoc />
    public string Name => "Realtime";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var signalR = services.AddSignalR(options =>
        {
            // Surfacing exception messages to clients would leak internals to
            // anyone with a browser console.
            options.EnableDetailedErrors = false;
        });

        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            signalR.AddStackExchangeRedis(redisConnectionString, options =>
                options.Configuration.ChannelPrefix = RedisChannel.Literal("enterprise-realtime")
            );

            services.TryAddRedisMultiplexer(redisConnectionString);
            services.AddSingleton<IConnectionRegistry, RedisConnectionRegistry>();
        }
        else
        {
            // Single-instance fallback so the framework runs without Redis in
            // development; presence would be per-replica in a scaled deploy.
            services.AddSingleton<IConnectionRegistry, InMemoryConnectionRegistry>();
        }

        services.AddSingleton<RealtimeDispatcher>();

        RegisterEventBridges(services);
    }

    /// <summary>
    /// Subscribes the bridge to every realtime event in the loaded modules.
    ///
    /// MediatR resolves <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>,
    /// whose generic argument is NESTED. The DI container can only map
    /// <c>IService&lt;T&gt;</c> to <c>Impl&lt;T&gt;</c>, so a single open-generic
    /// registration is impossible; instead the generic is closed once per
    /// discovered event type.
    ///
    /// Scanning loaded assemblies is safe here because the module loader
    /// touches every module assembly (resolving <c>typeof(XModule).Assembly</c>)
    /// before any <c>ConfigureServices</c> runs, so they are all loaded by now.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    private static void RegisterEventBridges(IServiceCollection services)
    {
        foreach (var eventType in DiscoverRealtimeEventTypes())
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            services.AddTransient(
                typeof(INotificationHandler<>).MakeGenericType(notificationType),
                typeof(RealtimeEventBridge<>).MakeGenericType(eventType)
            );
        }
    }

    /// <summary>
    /// Finds every concrete type implementing <see cref="IRealtimeEvent"/>.
    /// </summary>
    /// <returns>The realtime event types to bridge.</returns>
    private static IEnumerable<Type> DiscoverRealtimeEventTypes() =>
        AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(assembly =>
                assembly.GetName().Name?.StartsWith("EnterpriseFramework", StringComparison.Ordinal)
                == true
            )
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                typeof(IRealtimeEvent).IsAssignableFrom(type)
                && type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
            );

    /// <summary>
    /// Reads an assembly's types, tolerating ones that cannot be loaded.
    /// A single unloadable type must not stop the whole application from
    /// starting.
    /// </summary>
    /// <param name="assembly">Assembly to inspect.</param>
    /// <returns>The types that could be loaded.</returns>
    private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Select(type => type!);
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // The hub authenticates via [Authorize] on the hub class; the host
        // pipeline is configured to read the token from the query string,
        // because browsers cannot set headers on a WebSocket handshake.
        endpoints.MapHub<RealtimeHub>(HubPath);

        endpoints
            .MapGet(
                "/api/realtime/presence",
                async (IConnectionRegistry registry, CancellationToken ct) =>
                    TypedResults.Ok(await registry.GetOnlineUsersAsync(ct))
            )
            .WithName("realtimePresence")
            .WithSummary("Lists the accounts currently connected.")
            .WithTags("Realtime");
    }
}

/// <summary>
/// Registration helpers for the shared Redis connection.
/// </summary>
internal static class RedisRegistration
{
    /// <summary>
    /// Registers a shared <see cref="IConnectionMultiplexer"/> unless one is
    /// already present.
    ///
    /// The multiplexer is expensive and thread-safe: creating a second one
    /// per feature is a common and costly mistake, so this checks first and
    /// other modules can reuse the same instance.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="connectionString">Redis connection string.</param>
    public static void TryAddRedisMultiplexer(
        this IServiceCollection services,
        string connectionString
    )
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IConnectionMultiplexer)))
        {
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(connectionString)
        );
    }
}
