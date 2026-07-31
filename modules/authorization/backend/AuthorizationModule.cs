using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Authorization.Features.Roles;
using EnterpriseFramework.Modules.Authorization.Persistence;
using EnterpriseFramework.Modules.Authorization.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Modules.Authorization;

/// <summary>
/// Authorization module: roles, permissions and the claims that carry them.
///
/// Depends on Auth (declared in module.json) because it enriches the tokens
/// Auth issues and reacts to its registration event — but references only
/// Auth's CONTRACTS assembly, never its implementation.
///
/// Endpoints here are protected by the very permissions this module defines,
/// which is the intended dogfooding: the mechanism guards its own
/// administration surface.
/// </summary>
public sealed class AuthorizationModule : IModule
{
    /// <inheritdoc />
    public string Name => "Authorization";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<AuthorizationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        AuthorizationDbContext.Schema
                    )
            )
        );

        services.AddScoped<PermissionReader>();

        // The extension point that lets Auth embed roles and permissions in
        // its tokens without knowing this module exists.
        services.AddScoped<IUserClaimsEnricher, RoleClaimsEnricher>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AuthorizationModule).Assembly)
        );
        services.AddValidatorsFromAssembly(typeof(AuthorizationModule).Assembly);

        // Migration AND seeding are startup writes, so they share one switch:
        // no instance boot ever mutates a production database by surprise.
        // Production runs both as an explicit release step.
        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<AuthorizationSeeder>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/authorization").WithTags("Authorization");

        group
            .MapGet(
                "/me",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new GetMyAuthorizationQuery(), ct))
            )
            .WithName("authorizationMe")
            .WithSummary("Returns the caller's effective roles and permissions.");

        group
            .MapGet(
                "/roles",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new ListRolesQuery(), ct))
            )
            .WithName("authorizationListRoles")
            .WithSummary("Lists every role with its permissions.")
            .RequirePermission(Permissions.Roles.Read);

        group
            .MapPut(
                "/users/{userId:guid}/roles",
                async (
                    Guid userId,
                    SetUserRolesRequest body,
                    ISender sender,
                    CancellationToken ct
                ) =>
                {
                    await sender.Send(new SetUserRolesCommand(userId, body.RoleNames), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("authorizationSetUserRoles")
            .WithSummary("Replaces the roles assigned to an account.")
            .RequirePermission(Permissions.Roles.Write)
            .ProducesValidationProblem();

        group
            .MapGet(
                "/permissions",
                () => TypedResults.Ok(Permissions.All)
            )
            .WithName("authorizationListPermissions")
            .WithSummary("Lists the permission catalogue known to the framework.")
            .RequirePermission(Permissions.Roles.Read);
    }
}

/// <summary>
/// Body of the "set user roles" endpoint.
/// </summary>
/// <param name="RoleNames">The complete new set of role names.</param>
public sealed record SetUserRolesRequest(IReadOnlyList<string> RoleNames);
