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
/// The public-file capability of the Storage module.
///
/// Two assertions carry the whole design: a file uploaded as public downloads
/// WITHOUT a token, and one uploaded without saying anything does not. The
/// second is the one that must never regress — the default decides what
/// happens when someone forgets the parameter.
/// </summary>
public sealed class StorageVisibilityTests : IClassFixture<PostgresApiFactory>
{
    private static readonly byte[] PngBytes =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A,
        0x00,
        0x00,
        0x00,
        0x0D,
        0x49,
        0x48,
        0x44,
        0x52,
    ];

    private readonly PostgresApiFactory _factory;

    public StorageVisibilityTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    /// <summary>Uploads a small PNG, optionally declaring it public.</summary>
    private static async Task<(Guid Id, string Visibility)> UploadAsync(
        HttpClient client,
        string? visibility
    )
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "photo.png");

        if (visibility is not null)
        {
            form.Add(new StringContent(visibility), "visibility");
        }

        var response = await client.PostAsync(new Uri("/api/files", UriKind.Relative), form);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        return (
            body.RootElement.GetProperty("id").GetGuid(),
            body.RootElement.GetProperty("visibility").GetString()!
        );
    }

    [Fact]
    public async Task APublicFileDownloadsWithoutAToken()
    {
        var admin = await AdminClientAsync();
        var (id, visibility) = await UploadAsync(admin, "Public");

        // The enum travels as a name, not as 0/1.
        visibility.ShouldBe("Public");

        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync(
            new Uri($"/api/files/public/{id}", UriKind.Relative)
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        // Served with its real type and inline, or an <img> on the showcase
        // would download the picture instead of showing it.
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");
        response.Content.Headers.ContentDisposition?.DispositionType.ShouldNotBe("attachment");
        response.Headers.TryGetValues("X-Content-Type-Options", out var nosniff).ShouldBeTrue();
        nosniff.ShouldContain("nosniff");
    }

    [Fact]
    public async Task AFileUploadedWithoutSayingAnythingStaysPrivate()
    {
        var admin = await AdminClientAsync();
        var (id, visibility) = await UploadAsync(admin, visibility: null);

        visibility.ShouldBe("Private");

        var anonymous = _factory.CreateClient();

        // Not on the public route...
        var viaPublicRoute = await anonymous.GetAsync(
            new Uri($"/api/files/public/{id}", UriKind.Relative)
        );
        // 404, not 403: a private file must be indistinguishable from one that
        // does not exist.
        viaPublicRoute.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // ...and not on the authenticated one either, without a token.
        var viaPrivateRoute = await anonymous.GetAsync(
            new Uri($"/api/files/{id}", UriKind.Relative)
        );
        viaPrivateRoute.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Its owner can still read it.
        var owner = await admin.GetAsync(new Uri($"/api/files/{id}", UriKind.Relative));
        owner.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnHtmlPayloadIsNeverServedAsHtml()
    {
        var admin = await AdminClientAsync();

        using var form = new MultipartFormDataContent();
        // The attack: an HTML document declared as an image. Echoing the
        // declared type would run this script on our own origin.
        var file = new ByteArrayContent(
            Encoding.UTF8.GetBytes("<!doctype html><script>alert(1)</script>")
        );
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "photo.png");
        form.Add(new StringContent("Public"), "visibility");

        var upload = await admin.PostAsync(new Uri("/api/files", UriKind.Relative), form);
        using var body = await ReadJson(upload);
        var id = body.RootElement.GetProperty("id").GetGuid();

        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync(
            new Uri($"/api/files/public/{id}", UriKind.Relative)
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/octet-stream");
        response.Content.Headers.ContentDisposition?.DispositionType.ShouldBe("attachment");
    }

    /// <summary>
    /// Registers an account, promotes it to Admin and signs in again so the
    /// new permissions are present in the token.
    /// </summary>
    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"storage-{Guid.NewGuid():N}@example.com";

        var registered = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Storage Tester", password = "Str0ngPassphrase" })
        );
        registered.StatusCode.ShouldBe(HttpStatusCode.OK);

        Guid userId;
        using (var body = await ReadJson(registered))
        {
            userId = body.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<Modules.Authorization.Persistence.AuthorizationDbContext>();
            var adminRole = dbContext.Roles.Single(role =>
                role.Name == Modules.Authorization.Domain.BuiltInRoles.Admin
            );
            dbContext.UserRoles.Add(
                Modules.Authorization.Domain.UserRole.Grant(userId, adminRole.Id)
            );
            await dbContext.SaveChangesAsync();
        }

        var login = await client.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email, password = "Str0ngPassphrase" })
        );
        using var loginBody = await ReadJson(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginBody.RootElement.GetProperty("accessToken").GetString()
        );

        return client;
    }
}
