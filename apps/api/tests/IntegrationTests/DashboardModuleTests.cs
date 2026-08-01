using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// End-to-end behavior of the Dashboard module: it aggregates whatever the
/// installed modules contribute, and each contributor decides for itself
/// whether the caller may see its tile.
/// </summary>
public sealed class DashboardModuleTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public DashboardModuleTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<HttpClient> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(
                new
                {
                    email = $"dashboard-{Guid.NewGuid():N}@example.com",
                    displayName = "Dashboard Tester",
                    password = "Str0ngPassphrase",
                }
            )
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );
        return client;
    }

    [Fact]
    public async Task Widgets_RequireAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/dashboard/widgets", UriKind.Relative)
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Widgets_OmitTilesTheCallerMayNotSee()
    {
        // A freshly registered account holds the User role only: no users.read,
        // no audit.read.
        var client = await RegisterAsync();

        var response = await client.GetAsync(
            new Uri("/api/dashboard/widgets", UriKind.Relative)
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ids = body
            .RootElement.EnumerateArray()
            .Select(widget => widget.GetProperty("id").GetString())
            .ToArray();

        // Absent, because the provider checks the permission itself — the
        // endpoint has no idea these tiles exist.
        ids.ShouldNotContain("users.total");
        ids.ShouldNotContain("audit.recent");

        // Present without any permission: unread notifications are the
        // caller's own data by definition.
        ids.ShouldContain("notifications.unread");
    }

    [Fact]
    public async Task Widgets_SerializeKindAsAName()
    {
        var client = await RegisterAsync();

        var response = await client.GetAsync(
            new Uri("/api/dashboard/widgets", UriKind.Relative)
        );

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var kind = body.RootElement[0].GetProperty("kind");

        // The SDK types this as "Stat" | "List". An ordinal here would mean the
        // generated client compares against 0 and 1, and reordering the enum
        // would silently change every stored and in-flight value.
        kind.ValueKind.ShouldBe(JsonValueKind.String);
        kind.GetString().ShouldBeOneOf("Stat", "List");
    }
}
