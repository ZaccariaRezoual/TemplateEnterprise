using System.Net;
using System.Text;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// The public contact endpoint: the only anonymous endpoint that accepts free
/// text, and therefore the one whose defences are worth asserting.
/// </summary>
public sealed class SiteContactTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public SiteContactTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// A host whose contact budget is set for the test at hand.
    ///
    /// Each call builds its own server, and therefore its own rate-limiting
    /// state: sharing one would make every test depend on which ran first —
    /// the limiter counts per IP, and in-memory tests are all the same client.
    /// </summary>
    private HttpClient CreateClient(int permitLimit) =>
        _factory
            .WithWebHostBuilder(builder =>
                builder.UseSetting(
                    "Modules:Site:ContactPermitLimit",
                    permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                )
            )
            .CreateClient();

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static object Message(string? website = null) =>
        new
        {
            name = "Ada",
            email = $"ada-{Guid.NewGuid():N}@example.com",
            body = "Hello from the integration tests.",
            website,
        };

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, object body) =>
        client.PostAsync(new Uri("/api/site/contact", UriKind.Relative), Json(body));

    [Fact]
    public async Task AnAnonymousVisitorCanWrite()
    {
        // No token: the whole point of the endpoint is that someone without an
        // account can reach us.
        var client = CreateClient(permitLimit: 50);

        var response = await PostAsync(client, Message());

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task AFilledHoneypot_IsAcceptedAndDropped()
    {
        var client = CreateClient(permitLimit: 50);

        var response = await PostAsync(client, Message(website: "http://spam.example"));

        // Indistinguishable from success on purpose: a rejection would tell
        // whoever wrote the bot which field to leave alone next time.
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("", "ada@example.com", "Hi")]
    [InlineData("Ada", "not-an-email", "Hi")]
    [InlineData("Ada", "ada@example.com", "")]
    public async Task RejectsMalformedInput(string name, string email, string body)
    {
        var client = CreateClient(permitLimit: 50);

        var response = await PostAsync(client, new { name, email, body });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BeyondItsOwnBudget_TheEndpointAnswers429()
    {
        // Its own policy, far tighter than the global one — the fixture
        // relaxes the global limits, so a 429 here can only come from the
        // contact policy.
        var client = CreateClient(permitLimit: 2);
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var response = await PostAsync(client, Message());
            statuses.Add(response.StatusCode);
        }

        statuses.ShouldContain(HttpStatusCode.TooManyRequests);
    }
}
