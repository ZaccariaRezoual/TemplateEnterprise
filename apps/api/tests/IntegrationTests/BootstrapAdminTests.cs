using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using EnterpriseFramework.Modules.Authorization.Domain;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// The bootstrap administrator: the account that makes a fresh installation
/// usable, created by Auth and promoted by Authorization through the event bus.
/// </summary>
public sealed class BootstrapAdminTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public BootstrapAdminTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<HttpClient> SignInAsAdminAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email = "admin@example.com", password = "Password123!" })
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
    public async Task TheSeededAccount_CanSignIn()
    {
        // The whole point: no SQL, no manual promotion — the credentials in
        // configuration simply work on a fresh database.
        var client = await SignInAsAdminAsync();

        client.DefaultRequestHeaders.Authorization.ShouldNotBeNull();
    }

    [Fact]
    public async Task TheSeededAccount_HoldsTheAdminRoleAndEveryPermission()
    {
        var client = await SignInAsAdminAsync();

        var response = await client.GetAsync(new Uri("/api/authorization/me", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var roles = body
            .RootElement.GetProperty("roles")
            .EnumerateArray()
            .Select(role => role.GetString())
            .ToArray();
        var permissions = body
            .RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Select(permission => permission.GetString())
            .ToArray();

        // Granted by Authorization reacting to Auth's BootstrapAdminSeeded:
        // neither module references the other.
        roles.ShouldContain(BuiltInRoles.Admin);
        permissions.ShouldBe(Permissions.All, ignoreOrder: true);
    }

    [Fact]
    public async Task TheSeededAccount_CanAdministerUsers()
    {
        var client = await SignInAsAdminAsync();

        // A permission a plain account does not hold: proof the promotion is
        // enforced by the API, not merely reported by /me.
        var response = await client.GetAsync(new Uri("/api/users?page=1&pageSize=1", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ANewRegistration_GetsTheBasicUserRoleOnly()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(
                new
                {
                    email = $"basic-{Guid.NewGuid():N}@example.com",
                    displayName = "Basic User",
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

        var me = await client.GetAsync(new Uri("/api/authorization/me", UriKind.Relative));
        using var meBody = JsonDocument.Parse(await me.Content.ReadAsStringAsync());

        var roles = meBody
            .RootElement.GetProperty("roles")
            .EnumerateArray()
            .Select(role => role.GetString())
            .ToArray();

        roles.ShouldBe([BuiltInRoles.BasicUser]);
    }
}
