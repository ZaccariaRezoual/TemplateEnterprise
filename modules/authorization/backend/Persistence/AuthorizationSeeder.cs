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

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
