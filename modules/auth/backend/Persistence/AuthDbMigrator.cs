using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Auth.Persistence;

/// <summary>
/// Applies this module's pending migrations at startup.
///
/// Registered only when "Modules:Auth:AutoMigrate" is true (the default, for
/// development and tests). Production pipelines disable it and run
/// `dotnet ef database update` as an explicit deployment step, so schema
/// changes never happen as a side effect of booting an instance.
/// </summary>
public sealed class AuthDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public AuthDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
