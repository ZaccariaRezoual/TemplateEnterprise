using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseFramework.IntegrationTests.Fixtures;

/// <summary>
/// In-memory host WITHOUT external dependencies: database work is disabled so
/// tests that only exercise routing, validation and middleware stay fast and
/// runnable anywhere (no Docker required).
/// </summary>
public sealed class LightweightApiFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Modules:Auth:AutoMigrate", "false");
        builder.UseSetting("Jwt:SigningKey", new string('t', 48));
    }
}
