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
/// End-to-end behavior of the supporting modules: settings layering,
/// notification delivery through the event bus, file round-trip and the
/// translation catalogue.
/// </summary>
public sealed class SupportModulesTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public SupportModulesTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private async Task<(HttpClient Client, Guid UserId)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(
                new
                {
                    email = $"support-{Guid.NewGuid():N}@example.com",
                    displayName = "Support Tester",
                    password = "Str0ngPassphrase",
                }
            )
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );
        return (client, body.RootElement.GetProperty("user").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Settings_FallBackToDeclaredDefaultsAndAcceptUserOverrides()
    {
        var (client, _) = await RegisterAsync();

        var initial = await client.GetAsync(new Uri("/api/settings", UriKind.Relative));
        initial.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var body = await ReadJson(initial))
        {
            var itemsPerPage = body
                .RootElement.EnumerateArray()
                .Single(s => s.GetProperty("key").GetString() == "ui.itemsPerPage");
            itemsPerPage.GetProperty("value").GetString().ShouldBe("25");
            itemsPerPage.GetProperty("isOverriddenByUser").GetBoolean().ShouldBeFalse();
        }

        var update = await client.PutAsync(
            new Uri("/api/settings/me/ui.itemsPerPage", UriKind.Relative),
            Json(new { value = "50" })
        );
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await client.GetAsync(new Uri("/api/settings", UriKind.Relative));
        using var updated = await ReadJson(after);
        var setting = updated
            .RootElement.EnumerateArray()
            .Single(s => s.GetProperty("key").GetString() == "ui.itemsPerPage");
        setting.GetProperty("value").GetString().ShouldBe("50");
        setting.GetProperty("isOverriddenByUser").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Settings_RejectAValueThatDoesNotMatchTheDeclaredType()
    {
        var (client, _) = await RegisterAsync();

        var response = await client.PutAsync(
            new Uri("/api/settings/me/ui.itemsPerPage", UriKind.Relative),
            Json(new { value = "not-a-number" })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Settings_RefuseAPerUserOverrideOfAGlobalOnlySetting()
    {
        var (client, _) = await RegisterAsync();

        var response = await client.PutAsync(
            new Uri("/api/settings/me/app.name", UriKind.Relative),
            Json(new { value = "Mine" })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Settings_GlobalWritesRequireThePermission()
    {
        var (client, _) = await RegisterAsync();

        var response = await client.PutAsync(
            new Uri("/api/settings/app.name", UriKind.Relative),
            Json(new { value = "Renamed" })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Notifications_AreCreatedFromTheEventBusAndScopedToTheirOwner()
    {
        var (client, userId) = await RegisterAsync();
        var (otherClient, _) = await RegisterAsync();

        // Published exactly as any module would, with no reference to the
        // Notifications module.
        using (var scope = _factory.Services.CreateScope())
        {
            var eventBus =
                scope.ServiceProvider.GetRequiredService<Application.Abstractions.IEventBus>();
            await eventBus.PublishAsync(
                new Modules.Notifications.Contracts.Events.NotificationRequested(
                    userId,
                    "Report ready",
                    "Your export finished."
                )
            );
        }

        var mine = await client.GetAsync(new Uri("/api/notifications", UriKind.Relative));
        using var body = await ReadJson(mine);
        body.RootElement.GetArrayLength().ShouldBe(1);
        body.RootElement[0].GetProperty("title").GetString().ShouldBe("Report ready");
        body.RootElement[0].GetProperty("isUnread").GetBoolean().ShouldBeTrue();

        // Another account must not see it.
        var theirs = await otherClient.GetAsync(new Uri("/api/notifications", UriKind.Relative));
        using var otherBody = await ReadJson(theirs);
        otherBody.RootElement.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Notifications_MarkReadAffectsOnlyTheCallersOwn()
    {
        var (client, userId) = await RegisterAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var eventBus =
                scope.ServiceProvider.GetRequiredService<Application.Abstractions.IEventBus>();
            await eventBus.PublishAsync(
                new Modules.Notifications.Contracts.Events.NotificationRequested(
                    userId,
                    "First",
                    "Body"
                )
            );
        }

        var markRead = await client.PostAsync(
            new Uri("/api/notifications/read", UriKind.Relative),
            Json(new { notificationId = (Guid?)null })
        );
        markRead.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var unread = await client.GetAsync(
            new Uri("/api/notifications?unreadOnly=true", UriKind.Relative)
        );
        using var body = await ReadJson(unread);
        body.RootElement.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Files_UploadRequiresThePermission()
    {
        var (client, _) = await RegisterAsync();

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent("hello"u8.ToArray()), "file", "note.txt" },
        };
        var response = await client.PostAsync(new Uri("/api/files", UriKind.Relative), content);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Localization_ServesCataloguesAnonymouslyAndFallsBack()
    {
        // Anonymous on purpose: the sign-in screen needs translations before
        // anyone can authenticate.
        var client = _factory.CreateClient();

        var italian = await client.GetAsync(
            new Uri("/api/localization/translations/it", UriKind.Relative)
        );
        italian.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var body = await ReadJson(italian))
        {
            body.RootElement.GetProperty("common.save").GetString().ShouldBe("Salva");
        }

        var unknown = await client.GetAsync(
            new Uri("/api/localization/translations/zz", UriKind.Relative)
        );
        using var fallback = await ReadJson(unknown);
        // An untranslated UI beats an empty one.
        fallback.RootElement.GetProperty("common.save").GetString().ShouldBe("Save");
    }
}
