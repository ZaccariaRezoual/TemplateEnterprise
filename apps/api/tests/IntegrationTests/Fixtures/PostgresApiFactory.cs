using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace EnterpriseFramework.IntegrationTests.Fixtures;

/// <summary>
/// In-memory host backed by a REAL PostgreSQL instance (Testcontainers).
///
/// Auth migrations run automatically against the container, so these tests
/// exercise the same SQL, schema and constraints as production — not an
/// in-memory approximation that would hide provider-specific bugs.
/// Shared per test class via <c>IClassFixture</c>: one container per class.
/// </summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    /// <inheritdoc />
    public async Task InitializeAsync() => await _postgres.StartAsync().ConfigureAwait(false);

    /// <inheritdoc />
    async Task IAsyncLifetime.DisposeAsync() =>
        await _postgres.DisposeAsync().ConfigureAwait(false);

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", new string('t', 48));
        // Tests own a throwaway database, so they opt into the startup
        // migration and seeding that production performs as a release step.
        builder.UseSetting("Modules:AutoMigrate", "true");
        // Generous budgets: rate limiting is asserted by its own test, not by
        // accident in unrelated flows (all tests share one client "IP" here).
        builder.UseSetting("RateLimiting:PermitLimit", "1000");
        builder.UseSetting("Modules:Auth:CredentialsPermitLimit", "1000");
        // Same bootstrap account development gets, so the tests exercise the
        // path a developer actually meets on a fresh database.
        builder.UseSetting("Modules:Auth:BootstrapAdmin:Enabled", "true");
    }
}
