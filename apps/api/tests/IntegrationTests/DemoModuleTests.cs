using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// End-to-end tests through the in-memory host: they prove the module system,
/// the MediatR pipeline, validation → ProblemDetails mapping and the
/// correlation id middleware, without external dependencies.
/// </summary>
public sealed class DemoModuleTests : IClassFixture<Fixtures.LightweightApiFactory>
{
    private readonly HttpClient _client;

    public DemoModuleTests(Fixtures.LightweightApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ping_ReturnsPongWithCorrelationHeader()
    {
        var response = await _client.GetAsync(new Uri("/api/demo/ping", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ShouldContain(h => h.Key == "X-Correlation-ID");

        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body.ShouldNotBeNull();
        body.RootElement.GetProperty("message").GetString().ShouldBe("pong");
    }

    [Fact]
    public async Task Echo_ReturnsEchoedText()
    {
        var response = await _client.PostAsJsonAsync(
            new Uri("/api/demo/echo", UriKind.Relative),
            new { text = "hello modules" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body.ShouldNotBeNull();
        body.RootElement.GetProperty("text").GetString().ShouldBe("hello modules");
        body.RootElement.GetProperty("length").GetInt32().ShouldBe("hello modules".Length);
    }

    [Fact]
    public async Task Echo_WithEmptyText_Returns400ProblemDetailsWithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(
            new Uri("/api/demo/echo", UriKind.Relative),
            new { text = "" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body.ShouldNotBeNull();
        body.RootElement.GetProperty("title").GetString().ShouldBe("Validation failed");
        body.RootElement.GetProperty("errors").TryGetProperty("Text", out _).ShouldBeTrue();
        body.RootElement.TryGetProperty("correlationId", out _).ShouldBeTrue();
    }
}
