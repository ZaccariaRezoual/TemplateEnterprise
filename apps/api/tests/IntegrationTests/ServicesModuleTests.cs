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
/// The Services module against a real PostgreSQL.
///
/// The test that matters most is <see cref="Drafts_NeverReachThePublicEndpoints"/>:
/// a draft on the public site is the one failure this module exists to
/// prevent, and it is a failure nothing else would report — the page would
/// simply work, for everyone.
/// </summary>
public sealed class ServicesModuleTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ServicesModuleTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    /// <summary>Body of the create/edit endpoints, with sensible defaults.</summary>
    private static object ServiceBody(
        string title,
        bool isPublished = true,
        bool isBookable = false,
        int? durationMinutes = null,
        string? slug = null,
        decimal? price = null,
        string? currency = null,
        int sortOrder = 0
    ) =>
        new
        {
            title,
            slug,
            shortDescription = "Una riga di presentazione.",
            description = "Il testo lungo della pagina di dettaglio.",
            durationMinutes,
            price,
            currency,
            isPublished,
            isBookable,
            sortOrder,
        };

    /// <summary>A title unique to this run, so tests never collide on the slug.</summary>
    private static string UniqueTitle(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    [Fact]
    public async Task AnAdministratorTakesAServiceFromNothingToPublished()
    {
        var client = await AdminClientAsync();
        var title = UniqueTitle("Consulenza");

        var created = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(title, isPublished: false))
        );
        created.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(created);
        var id = body.RootElement.GetProperty("id").GetGuid();
        var slug = body.RootElement.GetProperty("slug").GetString()!;

        // Derived from the title, without anyone typing it.
        slug.ShouldStartWith("consulenza-");
        body.RootElement.GetProperty("isPublished").GetBoolean().ShouldBeFalse();

        var published = await client.PutAsync(
            new Uri($"/api/admin/services/{id}", UriKind.Relative),
            Json(ServiceBody(title, isPublished: true, slug: slug))
        );
        published.StatusCode.ShouldBe(HttpStatusCode.OK);

        var anonymous = _factory.CreateClient();
        var page = await anonymous.GetAsync(new Uri($"/api/services/{slug}", UriKind.Relative));

        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var pageBody = await ReadJson(page);
        pageBody.RootElement.GetProperty("title").GetString().ShouldBe(title);
    }

    [Fact]
    public async Task Drafts_NeverReachThePublicEndpoints()
    {
        var client = await AdminClientAsync();
        var title = UniqueTitle("Bozza");

        var created = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(title, isPublished: false))
        );
        using var body = await ReadJson(created);
        var slug = body.RootElement.GetProperty("slug").GetString()!;

        var anonymous = _factory.CreateClient();

        var detail = await anonymous.GetAsync(new Uri($"/api/services/{slug}", UriKind.Relative));
        // 404, exactly like a slug that never existed: telling the two apart
        // would let anyone enumerate work in progress.
        detail.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var list = await anonymous.GetAsync(new Uri("/api/services", UriKind.Relative));
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var listBody = await ReadJson(list);
        listBody
            .RootElement.EnumerateArray()
            .Select(item => item.GetProperty("slug").GetString())
            .ShouldNotContain(slug);
    }

    [Fact]
    public async Task AnArchivedServiceLeavesTheShowcaseAndStaysUneditable()
    {
        var client = await AdminClientAsync();
        var title = UniqueTitle("Ritirato");

        var created = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(title))
        );
        using var body = await ReadJson(created);
        var id = body.RootElement.GetProperty("id").GetGuid();
        var slug = body.RootElement.GetProperty("slug").GetString()!;

        var archived = await client.PostAsync(
            new Uri($"/api/admin/services/{id}/archive", UriKind.Relative),
            content: null
        );
        archived.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var anonymous = _factory.CreateClient();
        var page = await anonymous.GetAsync(new Uri($"/api/services/{slug}", UriKind.Relative));
        page.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Archiving is a one-way door: editing it back into the showcase would
        // make the state meaningless, and its identifier may already be
        // referenced by an appointment.
        var edit = await client.PutAsync(
            new Uri($"/api/admin/services/{id}", UriKind.Relative),
            Json(ServiceBody(title, slug: slug))
        );
        edit.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        // But it is still readable in the administration, which is the whole
        // difference from deleting it.
        var stillThere = await client.GetAsync(
            new Uri($"/api/admin/services/{id}", UriKind.Relative)
        );
        stillThere.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ATypedSlugThatIsTakenIsRefusedRatherThanAltered()
    {
        var client = await AdminClientAsync();
        var slug = $"slug-{Guid.NewGuid():N}";

        var first = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(UniqueTitle("Primo"), slug: slug))
        );
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(UniqueTitle("Secondo"), slug: slug))
        );

        // Publishing "slug-2" to someone who asked for "slug" is worse than
        // telling them the address is taken.
        second.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ABookableServiceWithoutADurationIsRejected()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(UniqueTitle("Senza durata"), isBookable: true, durationMinutes: null))
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnImageCanBeAttachedToAServiceAndBecomesItsCover()
    {
        // A regression test with a specific shape in mind: attaching an image
        // adds a CHILD row through the navigation of a loaded service, which
        // is precisely the case where EF has to infer "new or existing?" from
        // the key. Get that wrong and the save fails on a perfectly ordinary
        // request — nothing else in this suite goes down that path.
        var client = await AdminClientAsync();

        var created = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(UniqueTitle("Con immagine")))
        );
        using var body = await ReadJson(created);
        var id = body.RootElement.GetProperty("id").GetGuid();

        var fileId = await UploadPublicImageAsync(client);

        var attached = await client.PostAsync(
            new Uri($"/api/admin/services/{id}/images", UriKind.Relative),
            Json(
                new
                {
                    storageFileId = fileId,
                    altText = "Sala riunioni luminosa",
                    sortOrder = 0,
                }
            )
        );

        attached.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var attachedBody = await ReadJson(attached);
        // The first image becomes the cover on its own: a card with no
        // thumbnail reads as broken.
        attachedBody.RootElement.GetProperty("isCover").GetBoolean().ShouldBeTrue();

        var reloaded = await client.GetAsync(
            new Uri($"/api/admin/services/{id}", UriKind.Relative)
        );
        using var reloadedBody = await ReadJson(reloaded);
        reloadedBody.RootElement.GetProperty("images").GetArrayLength().ShouldBe(1);
    }

    /// <summary>Uploads a small PNG marked public, as the admin UI does.</summary>
    private static async Task<Guid> UploadPublicImageAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D]
        );
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "photo.png");
        form.Add(new StringContent("Public"), "visibility");

        var response = await client.PostAsync(new Uri("/api/files", UriKind.Relative), form);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task TheAdministrationRefusesACallerWithoutThePermission()
    {
        var (client, _, _) = await RegisterAsync();

        var list = await client.GetAsync(new Uri("/api/admin/services", UriKind.Relative));
        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var create = await client.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(ServiceBody(UniqueTitle("Non permesso")))
        );
        create.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TheAdministrationRefusesAnAnonymousCaller()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync(new Uri("/api/admin/services", UriKind.Relative));

        // 401 rather than 403: nobody has been identified yet.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheShowcaseIsReadableWithoutAToken()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync(new Uri("/api/services", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Registers an account and returns a client authenticated as it.</summary>
    private async Task<(HttpClient Client, Guid UserId, string Email)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = $"services-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Services Tester", password = "Str0ngPassphrase" })
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );

        return (client, body.RootElement.GetProperty("user").GetProperty("id").GetGuid(), email);
    }

    /// <summary>
    /// Registers an account, promotes it to Admin and signs in again so the
    /// new permissions are present in the token.
    /// </summary>
    private async Task<HttpClient> AdminClientAsync()
    {
        var (client, userId, email) = await RegisterAsync();

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
        using var body = await ReadJson(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );

        return client;
    }
}
