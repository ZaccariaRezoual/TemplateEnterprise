using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// End-to-end auth flows against a real PostgreSQL: registration, login,
/// bearer-protected access, refresh rotation with reuse detection and logout.
/// These are the security guarantees of the framework, so they are asserted
/// against real HTTP semantics (cookies, headers, status codes), never by
/// calling handlers directly.
/// </summary>
public sealed class AuthFlowTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AuthFlowTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static string? RefreshCookieOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(c => c.StartsWith("ef_refresh=", StringComparison.Ordinal))
            : null;

    [Fact]
    public async Task Register_SignsInAndSetsAnHttpOnlyStrictRefreshCookie()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email = UniqueEmail(), displayName = "Ada", password = "Str0ngPassphrase" })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = await ReadJson(response);
        body.RootElement.GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
        body.RootElement.GetProperty("user").GetProperty("displayName").GetString().ShouldBe("Ada");

        var cookie = RefreshCookieOf(response);
        cookie.ShouldNotBeNull();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/auth");
    }

    [Fact]
    public async Task Register_RejectsADuplicateEmailAs422()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        var payload = new { email, displayName = "Ada", password = "Str0ngPassphrase" };

        (await client.PostAsync(new Uri("/api/auth/register", UriKind.Relative), Json(payload)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(payload)
        );

        second.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_ReturnsTheSameErrorForUnknownEmailAndWrongPassword()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Ada", password = "Str0ngPassphrase" })
        );

        var unknownEmail = await client.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email = UniqueEmail(), password = "Str0ngPassphrase" })
        );
        var wrongPassword = await client.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email, password = "WrongPassphrase1" })
        );

        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        // Identical error content (correlation ids aside): no account
        // enumeration through error details.
        using var unknownBody = await ReadJson(unknownEmail);
        using var wrongBody = await ReadJson(wrongPassword);
        unknownBody
            .RootElement.GetProperty("detail")
            .GetString()
            .ShouldBe(wrongBody.RootElement.GetProperty("detail").GetString());
        unknownBody
            .RootElement.GetProperty("title")
            .GetString()
            .ShouldBe(wrongBody.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Me_RequiresABearerTokenAndReturnsTheCallerProfile()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        var register = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Ada", password = "Str0ngPassphrase" })
        );
        using var session = await ReadJson(register);
        var accessToken = session.RootElement.GetProperty("accessToken").GetString();

        // Without a token: the secure-by-default fallback policy rejects.
        (await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        var me = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var profile = await ReadJson(me);
        profile.RootElement.GetProperty("email").GetString().ShouldBe(email);
        // Roles are NOT part of the auth profile: they belong to the
        // Authorization module and are served by its own endpoint.
        profile.RootElement.TryGetProperty("roles", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Refresh_RotatesTheTokenAndDetectsReuseByRevokingTheFamily()
    {
        // HandleCookies=false: the test manages cookies explicitly to be able
        // to replay an OLD token — exactly what an attacker would do.
        var client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = false,
            }
        );

        var register = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email = UniqueEmail(), displayName = "Ada", password = "Str0ngPassphrase" })
        );
        var firstCookie = CookiePair(RefreshCookieOf(register)!);

        // Legitimate rotation succeeds and hands out a NEW cookie.
        var refresh = await SendRefresh(client, firstCookie);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondCookie = CookiePair(RefreshCookieOf(refresh)!);
        secondCookie.ShouldNotBe(firstCookie);

        // Replaying the OLD token is reuse: rejected...
        (await SendRefresh(client, firstCookie)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // ...and the whole family is revoked, so even the NEW token dies.
        (await SendRefresh(client, secondCookie)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshTokenAndClearsTheCookie()
    {
        var client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = false,
            }
        );
        var register = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email = UniqueEmail(), displayName = "Ada", password = "Str0ngPassphrase" })
        );
        var cookie = CookiePair(RefreshCookieOf(register)!);

        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/auth/logout", UriKind.Relative)
        );
        logoutRequest.Headers.Add("Cookie", cookie);
        var logout = await client.SendAsync(logoutRequest);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The revoked token can no longer be exchanged.
        (await SendRefresh(client, cookie)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpResponseMessage> SendRefresh(HttpClient client, string cookie)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/auth/refresh", UriKind.Relative)
        );
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    /// <summary>Extracts "name=value" from a full Set-Cookie header line.</summary>
    private static string CookiePair(string setCookie) => setCookie.Split(';')[0];
}
