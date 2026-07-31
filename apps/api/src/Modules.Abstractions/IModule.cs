using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Modules.Abstractions;

/// <summary>
/// Contract every backend module must implement (the "Module Contract").
///
/// A module is a self-contained feature (Auth, Users, Notifications, ...) that
/// plugs into the host without the host knowing its internals. The loader
/// (<see cref="ModuleLoader"/>) discovers implementations, reads their
/// <c>module.json</c> manifest, resolves dependency order and invokes the two
/// lifecycle methods below. Disabling a module in configuration removes its
/// services and endpoints without touching any other module.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Module name; MUST match the "name" field of the module's
    /// <c>module.json</c> manifest (used for dependency resolution and the
    /// "Modules:{Name}:Enabled" configuration override).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers the module services (MediatR handlers, validators, own
    /// services). Called once at startup, before the container is built.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="configuration">Host configuration (read module settings from "Modules:{Name}").</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Maps the module HTTP endpoints. Called once at startup, after the
    /// container is built. Group endpoints under "/api/{module}".
    /// </summary>
    /// <param name="endpoints">Host endpoint route builder.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
