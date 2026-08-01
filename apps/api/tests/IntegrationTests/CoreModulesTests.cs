using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// Verifies the architectural claims the core modules make, against a real
/// PostgreSQL:
///
/// - Auth's registration event reaches OTHER modules: Authorization grants the
///   default role and Users builds its profile projection.
/// - Permission enforcement is real (403 without the permission, 200 with it)
///   and comes from token claims contributed by the Authorization module.
/// - The Audit module records commands and login events without the audited
///   modules knowing it exists.
/// </summary>
public sealed class CoreModulesTests : IClassFixture<PostgresApiFactory>
{
    private static readonly string[] AdminRole = [Modules.Authorization.Domain.BuiltInRoles.Admin];

    private readonly PostgresApiFactory _factory;

    public CoreModulesTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() => $"core-{Guid.NewGuid():N}@example.com";

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    /// <summary>Registers an account and returns a client authenticated as it.</summary>
    private async Task<(HttpClient Client, Guid UserId, string Email)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Core Tester", password = "Str0ngPassphrase" })
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );
        var userId = body.RootElement.GetProperty("user").GetProperty("id").GetGuid();

        return (client, userId, email);
    }

    [Fact]
    public async Task Registration_GrantsTheDefaultRoleThroughTheAuthorizationModule()
    {
        var (client, _, _) = await RegisterAsync();

        var response = await client.GetAsync(new Uri("/api/authorization/me", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = await ReadJson(response);
        body.RootElement.GetProperty("roles")
            .EnumerateArray()
            .Select(r => r.GetString())
            .ShouldContain(Modules.Authorization.Domain.BuiltInRoles.BasicUser);
        // The default role carries no administration permissions.
        body.RootElement.GetProperty("permissions").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Registration_BuildsTheUsersProjectionFromTheAuthEvent()
    {
        // The Users module never reads Auth's tables: the profile can only
        // exist because the registration event crossed the module boundary.
        var (adminClient, _, _) = await RegisterAsAdminAsync();
        var (_, _, memberEmail) = await RegisterAsync();

        var response = await adminClient.GetAsync(
            new Uri($"/api/users?search={Uri.EscapeDataString(memberEmail)}", UriKind.Relative)
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = await ReadJson(response);
        var items = body.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBe(1);
        items[0].GetProperty("email").GetString().ShouldBe(memberEmail);
        items[0].GetProperty("displayName").GetString().ShouldBe("Core Tester");
    }

    [Fact]
    public async Task ProtectedEndpoint_Denies403WithoutThePermission()
    {
        var (client, _, _) = await RegisterAsync();

        var response = await client.GetAsync(new Uri("/api/users", UriKind.Relative));

        // Authenticated but not permitted: 403, not 401.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProtectedEndpoint_AllowsOnceThePermissionIsGrantedAndTheTokenRefreshed()
    {
        var (adminClient, _, _) = await RegisterAsAdminAsync();

        var response = await adminClient.GetAsync(new Uri("/api/users", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RoleAdministration_ItselfRequiresThePermission()
    {
        var (memberClient, memberId, _) = await RegisterAsync();

        var response = await memberClient.PutAsync(
            new Uri($"/api/authorization/users/{memberId}/roles", UriKind.Relative),
            Json(new { roleNames = AdminRole })
        );

        // A user cannot promote themselves: the module's own administration
        // surface is guarded by the permissions it defines.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Audit_RecordsCommandsAndLoginsFromModulesThatIgnoreIt()
    {
        var (adminClient, adminId, adminEmail) = await RegisterAsAdminAsync();

        // A login produces an event-sourced entry...
        await adminClient.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email = adminEmail, password = "Str0ngPassphrase" })
        );

        var response = await adminClient.GetAsync(new Uri("/api/audit?take=200", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        var entries = body.RootElement.EnumerateArray().ToArray();

        // ...and the commands that ran are recorded automatically by the
        // pipeline behavior, with no auditing code in Auth or Authorization.
        entries
            .Select(e => e.GetProperty("action").GetString())
            .ShouldContain("RegisterCommand");
        entries.Select(e => e.GetProperty("action").GetString()).ShouldContain("UserLoggedIn");
        entries
            .Where(e => e.GetProperty("action").GetString() == "SetUserRolesCommand")
            .ShouldAllBe(e => e.GetProperty("succeeded").GetBoolean());
        entries.ShouldContain(e =>
            e.GetProperty("userId").ValueKind != JsonValueKind.Null
            && e.GetProperty("userId").GetGuid() == adminId
        );
    }

    /// <summary>
    /// Registers an account, promotes it to Admin directly in the
    /// database-backed module, then signs in again so the new permissions are
    /// present in the token.
    /// </summary>
    private async Task<(HttpClient Client, Guid UserId, string Email)> RegisterAsAdminAsync()
    {
        var (client, userId, email) = await RegisterAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<Modules.Authorization.Persistence.AuthorizationDbContext>();
            var adminRole = dbContext.Roles.Single(r =>
                r.Name == Modules.Authorization.Domain.BuiltInRoles.Admin
            );
            dbContext.UserRoles.Add(
                Modules.Authorization.Domain.UserRole.Grant(userId, adminRole.Id)
            );
            await dbContext.SaveChangesAsync();
        }

        // Permissions live in the access token, so they only take effect on
        // the next issued token — exactly the staleness window the design
        // documents.
        var login = await client.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email, password = "Str0ngPassphrase" })
        );
        using var body = await ReadJson(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );

        return (client, userId, email);
    }
}
