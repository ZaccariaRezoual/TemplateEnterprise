using EnterpriseFramework.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Application;

/// <summary>
/// Composition entry point of the Application layer.
///
/// Registers MediatR with the framework-wide pipeline behaviors
/// (logging, validation). Modules register their own handlers/validators in
/// their <c>IModule.ConfigureServices</c>; the behaviors registered here apply
/// to every module automatically.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Application layer services to the container.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly)
        );

        // Order matters: logging wraps validation so failed validations are logged too.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
