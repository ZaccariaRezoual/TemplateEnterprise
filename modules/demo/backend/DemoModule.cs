using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Demo.Features.Echo;
using EnterpriseFramework.Modules.Demo.Features.Ping;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // TypedResults (not Results) is required: it carries the response type
        // into the OpenAPI document, which is what makes the generated SDK
        // fully typed. WithName sets the operationId the SDK uses as the
        // method name, so renaming a handler never renames a public SDK method.
        group
            .MapGet(
                "/ping",
                async (ISender sender, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await sender.Send(new PingQuery(), cancellationToken))
            )
            .WithName("demoPing")
            .WithSummary("Returns a static pong payload with the server UTC time.");

        group
            .MapPost(
                "/echo",
                async (EchoCommand command, ISender sender, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await sender.Send(command, cancellationToken))
            )
            .WithName("demoEcho")
            .WithSummary("Echoes the given text and publishes a domain event.")
            .ProducesValidationProblem();
    }
}
