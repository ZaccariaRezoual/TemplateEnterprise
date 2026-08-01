using EnterpriseFramework.Modules.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Authorization.Persistence;

/// <summary>
/// Applies migrations and seeds the built-in roles at startup.
///
/// Idempotent by design: it creates missing roles and re-syncs the
/// permissions of built-in ones, so adding a permission to the catalogue in
/// code reaches existing databases on the next boot. Custom roles are never
/// touched.
/// </summary>
public sealed class AuthorizationSeeder : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the seeder.
    /// </summary>
    /// <param name="services">Root provider used to create a startup scope.</param>
    public AuthorizationSeeder(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthorizationDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (name, description, permissions) in BuiltInRoles.Definitions)
        {
            var existing = await dbContext
                .Roles.SingleOrDefaultAsync(r => r.Name == name, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                dbContext.Roles.Add(Role.Create(name, description, permissions, isBuiltIn: true));
            }
            else if (existing.IsBuiltIn)
            {
                // Keep built-in roles in sync with the catalogue in code.
                existing.SetPermissions(permissions);
            }
        }

        // Built-in roles are owned by the catalogue in code, so one that is no
        // longer declared must not survive: renaming a role would otherwise
        // leave the old one behind forever, still granting its permissions to
        // whoever holds it. Custom roles have IsBuiltIn = false and are never
        // touched.
        var declared = BuiltInRoles.Definitions.Select(definition => definition.Name).ToArray();
        var stale = await dbContext
            .Roles.Where(role => role.IsBuiltIn && !declared.Contains(role.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (stale.Count > 0)
        {
            var staleIds = stale.Select(role => role.Id).ToArray();
            dbContext.UserRoles.RemoveRange(
                dbContext.UserRoles.Where(grant => staleIds.Contains(grant.RoleId))
            );
            dbContext.Roles.RemoveRange(stale);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
