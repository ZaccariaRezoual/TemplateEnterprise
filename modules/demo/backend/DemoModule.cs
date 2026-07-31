using EnterpriseFramework.Modules.Demo.Features.Echo;
using EnterpriseFramework.Modules.Demo.Features.Ping;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseFramework.Modules.Abstractions;

namespace EnterpriseFramework.Modules.Demo;

/// <summary>
/// Reference implementation of the Module Contract.
///
/// Responsibilities:
/// - Registers the module's MediatR handlers and FluentValidation validators.
/// - Maps the module endpoints under "/api/demo".
///
/// Copy this class as the starting point when creating a new module; it will
/// be removed once real modules (Auth, Users, ...) exist.
/// </summary>
public sealed class DemoModule : IModule
{
    /// <inheritdoc />
    public string Name => "Demo";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DemoModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(DemoModule).Assembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/demo").WithTags("Demo");

        group.MapGet(
            "/ping",
            async (ISender sender, CancellationToken cancellationToken) =>
                Results.Ok(await sender.Send(new PingQuery(), cancellationToken))
        );

        group.MapPost(
            "/echo",
            async (EchoCommand command, ISender sender, CancellationToken cancellationToken) =>
                Results.Ok(await sender.Send(command, cancellationToken))
        );
    }
}
