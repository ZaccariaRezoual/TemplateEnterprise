using System.Net;
using System.Text;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// Asserts the brute-force containment on credential endpoints: past the
/// configured budget the limiter answers 429 BEFORE the endpoint runs.
/// Uses its own host with a tiny budget so the other flows stay unaffected.
/// </summary>
public sealed class AuthRateLimitTests : IClassFixture<LightweightApiFactory>
{
    private readonly LightweightApiFactory _factory;

    public AuthRateLimitTests(LightweightApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginAttemptsBeyondTheBudget_Receive429()
    {
        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.UseSetting("Modules:Auth:SensitivePermitLimit", "2")
            )
            .CreateClient();

        var payload = new StringContent(
            JsonSerializer.Serialize(new { email = "a@b.c", password = "x" }),
            Encoding.UTF8,
            "application/json"
        );

        // The first two attempts consume the budget (their outcome is irrelevant here).
        await client.PostAsync(new Uri("/api/auth/login", UriKind.Relative), payload);
        await client.PostAsync(new Uri("/api/auth/login", UriKind.Relative), payload);
        var third = await client.PostAsync(new Uri("/api/auth/login", UriKind.Relative), payload);

        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
