using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Stockly.Api.Common;
using Xunit;

namespace Stockly.UnitTests.Common;

/// <summary>
/// <c>SECURITY_ARCHITECTURE.md</c> §12. The headers are asserted as exact strings
/// because a weakened value — <c>frame-ancestors *</c>, a missing
/// <c>object-src 'none'</c>, a <c>unsafe-inline</c> script source — is the defect the
/// table exists to prevent, and a "contains" assertion would not catch it.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    private static async Task<HttpContext> RunAsync(bool isHttps = false, bool withToken = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = isHttps ? "https" : "http";

        if (withToken)
        {
            context.Request.Headers.Authorization = "Bearer abc.def.ghi";
        }

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);
        return context;
    }

    [Theory]
    [InlineData("Content-Security-Policy")]
    [InlineData("X-Content-Type-Options")]
    [InlineData("X-Frame-Options")]
    [InlineData("Referrer-Policy")]
    [InlineData("Permissions-Policy")]
    [InlineData("Cross-Origin-Opener-Policy")]
    [InlineData("Cross-Origin-Resource-Policy")]
    public async Task The_header_is_present_on_an_anonymous_response(string header)
    {
        var context = await RunAsync();

        context.Response.Headers[header].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task The_content_security_policy_matches_the_specification_exactly()
    {
        var context = await RunAsync();

        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be(
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: blob:; "
            + "connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; "
            + "form-action 'self'");
    }

    [Fact]
    public async Task The_policy_forbids_framing_and_plugin_content()
    {
        var csp = (await RunAsync()).Response.Headers["Content-Security-Policy"].ToString();

        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("object-src 'none'");
        csp.Should().NotContain("*");
        csp.Should().NotContain("unsafe-inline");
    }

    [Fact]
    public async Task Nosniff_and_deny_are_set()
    {
        var context = await RunAsync();

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task The_referrer_policy_discloses_nothing()
    {
        (await RunAsync()).Response.Headers["Referrer-Policy"].ToString().Should().Be("no-referrer");
    }

    [Fact]
    public async Task The_permissions_policy_allows_camera_only_for_the_scanning_origin()
    {
        var value = (await RunAsync()).Response.Headers["Permissions-Policy"].ToString();

        value.Should().Be("camera=(self), microphone=(), geolocation=()");
    }

    [Fact]
    public async Task The_cross_origin_headers_are_same_origin()
    {
        var context = await RunAsync();

        context.Response.Headers["Cross-Origin-Opener-Policy"].ToString().Should().Be("same-origin");
        context.Response.Headers["Cross-Origin-Resource-Policy"].ToString().Should().Be("same-origin");
    }

    [Fact]
    public async Task Hsts_is_sent_over_tls()
    {
        (await RunAsync(isHttps: true)).Response.Headers["Strict-Transport-Security"].ToString()
            .Should().Be("max-age=31536000; includeSubDomains; preload");
    }

    [Fact]
    public async Task Hsts_is_not_sent_over_plain_http()
    {
        (await RunAsync()).Response.Headers.ContainsKey("Strict-Transport-Security")
            .Should().BeFalse();
    }

    [Fact]
    public async Task An_authenticated_response_is_not_cacheable()
    {
        var context = await RunAsync(withToken: true);

        context.Response.Headers["Cache-Control"].ToString().Should().Contain("no-store");
        context.Response.Headers["Pragma"].ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task An_anonymous_response_stays_cacheable()
    {
        (await RunAsync()).Response.Headers.ContainsKey("Cache-Control").Should().BeFalse();
    }

    [Fact]
    public async Task The_downstream_pipeline_runs_after_the_headers_are_set()
    {
        var called = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());
        called.Should().BeTrue();
    }

    [Fact]
    public async Task A_null_context_is_refused()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        var act = async () => await middleware.InvokeAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}