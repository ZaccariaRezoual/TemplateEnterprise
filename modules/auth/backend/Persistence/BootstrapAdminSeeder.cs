using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using EnterpriseFramework.Modules.Auth.Domain;
using EnterpriseFramework.Modules.Auth.Options;
using EnterpriseFramework.Modules.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Auth.Persistence;

/// <summary>
/// Creates the bootstrap administrator account at startup.
///
/// Responsibilities: creating the account if it does not exist, and announcing
/// it on the event bus so the other modules can react — Authorization grants
/// the Admin role, Users creates the profile projection.
///
/// It does NOT assign roles: this module has no concept of one. That
/// separation is the reason the account is announced rather than promoted.
///
/// Registered only when "Modules:Auth:BootstrapAdmin:Enabled" is true, which
/// the host defaults to Development. Idempotent: an existing account is left
/// exactly as it is, including a password or a role an administrator changed.
///
/// The work happens on <c>ApplicationStarted</c> rather than in
/// <c>StartAsync</c>, because hosted services run in module load order — a
/// DEPENDENCY graph, not a seeding order. Auth has no dependencies, so it
/// starts first, before Authorization has migrated its schema or seeded the
/// Admin role. Waiting until every module has started makes the outcome
/// independent of that order instead of quietly correct only today.
/// </summary>
public sealed partial class BootstrapAdminSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly BootstrapAdminOptions _options;
    private readonly ILogger<BootstrapAdminSeeder> _logger;

    [LoggerMessage(Level = LogLevel.Information, Message = "Bootstrap admin account created: {Email}")]
    private static partial void LogCreated(ILogger logger, string email);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Bootstrap admin not seeded: the configured password does not satisfy the password policy"
    )]
    private static partial void LogWeakPassword(ILogger logger);

    /// <summary>
    /// Initializes the seeder.
    /// </summary>
    /// <param name="services">Root provider used to create a startup scope.</param>
    /// <param name="lifetime">Used to defer seeding until every module has started.</param>
    /// <param name="options">Configured account details.</param>
    /// <param name="logger">Reports what was seeded, or why it was not.</param>
    public BootstrapAdminSeeder(
        IServiceProvider services,
        IHostApplicationLifetime lifetime,
        IOptions<BootstrapAdminOptions> options,
        ILogger<BootstrapAdminSeeder> logger
    )
    {
        _services = services;
        _lifetime = lifetime;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Blocking inside the callback is deliberate: the host runs these
        // before `StartAsync` returns, so the application never begins serving
        // requests — nor does an integration test get a client — with the
        // account half-created.
        _lifetime.ApplicationStarted.Register(() =>
            SeedAsync(_lifetime.ApplicationStopping).GetAwaiter().GetResult()
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Creates the account and announces it, unless it already exists.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!IsAcceptablePassword(_options.Password))
        {
            // Refusing beats creating an account that the password policy
            // would reject on a normal registration: the difference would only
            // surface much later, on a password change nobody can complete.
            LogWeakPassword(_logger);
            return;
        }

        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var normalized = _options.Email.ToUpperInvariant();
        var exists = await dbContext
            .Users.AnyAsync(user => user.NormalizedEmail == normalized, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = User.Register(
            _options.Email,
            _options.DisplayName,
            hasher.Hash(_options.Password)
        );

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // UserRegistered first: the profile projection and the default role
        // come from it, and the account should be a normal, complete account
        // before it is anything else.
        foreach (var domainEvent in admin.DomainEvents)
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
        admin.ClearDomainEvents();

        await eventBus
            .PublishAsync(new BootstrapAdminSeeded(admin.Id, admin.Email), cancellationToken)
            .ConfigureAwait(false);

        LogCreated(_logger, admin.Email);
    }

    /// <summary>
    /// Mirrors <c>RegisterCommandValidator</c>'s password rules. Duplicated
    /// deliberately: the validator belongs to a MediatR pipeline this seeder
    /// does not run through, and the rule is short enough that sharing it
    /// would cost more coupling than it saves.
    /// </summary>
    private static bool IsAcceptablePassword(string password) =>
        password.Length >= 12
        && password.Any(char.IsLower)
        && password.Any(char.IsUpper)
        && password.Any(char.IsDigit);
}
