using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Stockly.Api.Configuration;
using Stockly.Infrastructure.Outbox;
using Stockly.StocklyDbTestSupport;

namespace Stockly.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class ApiHostTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiHostTests(SqlServerFixture fixture)
    {
        fixture.RequireServer();

        _factory = new StocklyApiFactory(
            fixture.ConnectionString,
            fixture.EnsurePlatformPrincipalAsync().GetAwaiter().GetResult());
    }

    private sealed class StocklyApiFactory(string connectionString, string monitorConnectionString)
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var originalDatabase = Environment.GetEnvironmentVariable(DatabaseConnection.EnvironmentVariable);
            var originalMonitor = Environment.GetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable);
            var originalWorker = Environment.GetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable);
            var originalEnabled = Environment.GetEnvironmentVariable("Outbox__Enabled");
            Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, connectionString);
            Environment.SetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable, monitorConnectionString);
            Environment.SetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable, monitorConnectionString);
            Environment.SetEnvironmentVariable("Outbox__Enabled", "false");

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, originalDatabase);
                Environment.SetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable, originalMonitor);
                Environment.SetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable, originalWorker);
                Environment.SetEnvironmentVariable("Outbox__Enabled", originalEnabled);
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
        body.RootElement.GetProperty("outbox").GetString().Should().Be("ok");
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
