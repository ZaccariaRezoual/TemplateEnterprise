using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Users.Features.ListUsers;
using EnterpriseFramework.Modules.Users.Features.UpdateUser;
using EnterpriseFramework.Modules.Users.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Users;

/// <summary>
/// Users module: administration of user profiles.
///
/// Declares dependencies on Auth (whose events feed its projection) and
/// Authorization (whose permissions protect its endpoints); the module loader
/// refuses to start if either is disabled, which is the point of declaring
/// dependencies in module.json.
/// </summary>
public sealed class UsersModule : IModule
{
    /// <inheritdoc />
    public string Name => "Users";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", UsersDbContext.Schema)
            )
        );

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UsersModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(UsersModule).Assembly);

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<UsersDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").WithTags("Users");

        group
            .MapGet(
                "/",
                async (
                    ISender sender,
                    CancellationToken ct,
                    int page = 1,
                    int pageSize = 25,
                    string? search = null
                ) => TypedResults.Ok(await sender.Send(new ListUsersQuery(page, pageSize, search), ct))
            )
            .WithName("usersList")
            .WithSummary("Lists user profiles, paged and searchable.")
            .RequirePermission(Permissions.Users.Read)
            .ProducesValidationProblem();

        group
            .MapPut(
                "/{userId:guid}",
                async (Guid userId, UpdateUserRequest body, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(
                        await sender.Send(
                            new UpdateUserCommand(
                                userId,
                                body.DisplayName,
                                body.JobTitle,
                                body.IsActive
                            ),
                            ct
                        )
                    )
            )
            .WithName("usersUpdate")
            .WithSummary("Updates a user profile.")
            .RequirePermission(Permissions.Users.Write)
            .ProducesValidationProblem();
    }
}

/// <summary>
/// Body of the "update user" endpoint.
/// </summary>
/// <param name="DisplayName">New display name.</param>
/// <param name="JobTitle">New job title, or null to clear it.</param>
/// <param name="IsActive">Whether the account is active.</param>
public sealed record UpdateUserRequest(string DisplayName, string? JobTitle, bool IsActive);

/// <summary>
/// Applies this module's pending migrations at startup (development default;
/// production runs them as an explicit release step).
/// </summary>
public sealed class UsersDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public UsersDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
