using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Stockly.Api.Configuration;
using Stockly.StocklyDbTestSupport;

namespace Stockly.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class ApiHostTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiHostTests(SqlServerFixture fixture)
    {
        fixture.RequireServer();

        _factory = new StocklyApiFactory(fixture.ConnectionString);
    }

    private sealed class StocklyApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var original = Environment.GetEnvironmentVariable(DatabaseConnection.EnvironmentVariable);
            Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, connectionString);

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, original);
            }
        }
    }

    [Fact]
    public async Task Live_endpoint_returns_ok_with_correlation_and_security_headers()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        body.RootElement.GetProperty("status").GetString().Should().Be("live");
    }

    [Fact]
    public async Task Ready_endpoint_returns_ok_when_the_canonical_database_is_current()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        body.RootElement.GetProperty("status").GetString().Should().Be("ready");
        body.RootElement.GetProperty("database").GetString().Should().Be("ok");
        body.RootElement.GetProperty("migrations").GetString().Should().Be("current");
    }

    [Fact]
    public async Task OpenAPI_document_is_available_and_sanitised()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api-docs/openapi.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        body.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");
        body.RootElement.GetProperty("info").GetProperty("title").GetString().Should().Be("Stockly API");
        body.RootElement.GetProperty("info").GetProperty("version").GetString().Should().Be("v1");
        body.RootElement.TryGetProperty("servers", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Health_endpoints_are_anonymous()
    {
        var client = _factory.CreateClient();

        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");

        live.StatusCode.Should().Be(HttpStatusCode.OK);
        ready.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
