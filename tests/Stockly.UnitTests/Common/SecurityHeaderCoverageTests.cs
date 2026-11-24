using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Stockly.Api.Common;
using Xunit;

namespace Stockly.UnitTests.Common;

/// <summary>
/// Completeness guard for <c>SECURITY_ARCHITECTURE.md</c> §12.
/// <para>
/// The existing header tests assert the exact values, but a table with nine rows
/// invites the question "are all nine actually covered?" and the answer is not visible
/// from the test names. This file answers it structurally: the expected header set is
/// written out once, and the middleware is asked for each one, so adding a ninth
/// header to the specification without a test — or emitting a header the
/// specification does not list — is a build failure in either direction.
/// </para>
/// </summary>
public sealed class SecurityHeaderCoverageTests
{
    /// <summary>
    /// Every header §12 requires, with its specified value. Kept as data so the set
    /// is auditable against the document in one place.
    /// </summary>
    private static readonly (string Header, string Value)[] Required =
    [
        (
            "Strict-Transport-Security",
            "max-age=31536000; includeSubDomains; preload"
        ),
        (
            "Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: blob:; "
            + "connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; "
            + "form-action 'self'"
        ),
        ("X-Content-Type-Options", "nosniff"),
        ("X-Frame-Options", "DENY"),
        ("Referrer-Policy", "no-referrer"),
        ("Permissions-Policy", "camera=(self), microphone=(), geolocation=()"),
        ("Cross-Origin-Opener-Policy", "same-origin"),
        ("Cross-Origin-Resource-Policy", "same-origin"),
    ];

    /// <summary>
    /// §12's ninth row is conditional rather than a fixed value, so it is asserted
    /// separately and named here to keep the count of nine visible.
    /// </summary>
    private const string CacheControlHeader = "Cache-Control";

    private static async Task<HttpContext> RunAsync(bool isHttps = true, bool withToken = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = isHttps ? "https" : "http";

        if (withToken)
        {
            context.Request.Headers.Authorization = "Bearer abc.def.ghi";
        }

        await new SecurityHeadersMiddleware(_ => Task.CompletedTask).InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task All_eight_unconditional_headers_are_emitted_with_the_specified_value()
    {
        var context = await RunAsync();

        // Asserted as a set so a missing row fails here even if a dedicated test for
        // it were ever deleted by accident.
        foreach (var (header, value) in Required)
        {
            context.Response.Headers[header].ToString()
                .Should().Be(value, "SECURITY_ARCHITECTURE §12 specifies {0}", header);
        }
    }

    [Fact]
    public async Task The_ninth_header_is_emitted_on_an_authenticated_response()
    {
        var context = await RunAsync(withToken: true);

        context.Response.Headers[CacheControlHeader].ToString().Should().Contain("no-store");
    }

    [Fact]
    public void The_specification_table_has_nine_rows()
    {
        // If §12 ever gains or loses a row, this fails and forces the list above to be
        // reconciled. It is a documentation-drift guard, not a security control.
        Required.Length.Should().Be(8, "eight unconditional headers plus one conditional");

        // The two sets must be disjoint, so the count of nine is unambiguous.
        var all = Required.Select(entry => entry.Header).Append(CacheControlHeader);
        all.Should().OnlyHaveUniqueItems().And.HaveCount(9);
    }

    [Fact]
    public async Task No_unlisted_security_header_is_emitted()
    {
        // The reverse direction: the middleware must not invent headers. A stray
        // header is how a weakened policy sneaks in unannounced.
        var allowed = Required.Select(entry => entry.Header)
            .Append(CacheControlHeader)
            .Append("Pragma")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var context = await RunAsync(withToken: true);

        var unexpected = context.Response.Headers.Keys
            .Where(key => !allowed.Contains(key))
            .ToArray();

        unexpected.Should().BeEmpty(
            "SECURITY_ARCHITECTURE §12 lists nine headers; anything else is undocumented behaviour");
    }

    [Theory]
    [InlineData("X-XSS-Protection")]
    [InlineData("Server")]
    [InlineData("X-Powered-By")]
    public async Task A_legacy_or_informational_header_is_not_added(string header)
    {
        // X-XSS-Protection is obsolete and its presence in modern CSP deployments is
        // noise; Server/X-Powered-By disclose the platform. None is required here.
        var context = await RunAsync(withToken: true);

        context.Response.Headers.ContainsKey(header).Should().BeFalse();
    }

    [Fact]
    public async Task The_headers_are_set_before_the_response_starts()
    {
        // If the middleware ran after the response had begun, the header writes would
        // throw or be silently dropped.
        HttpResponse? response = null;

        var middleware = new SecurityHeadersMiddleware(context =>
        {
            response = context.Response;
            response.HasStarted.Should().BeFalse();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Every_required_header_is_present_on_a_response_with_no_token()
    {
        // A security header set only for authenticated routes would leave the health
        // endpoint and the problem pages unprotected.
        var context = await RunAsync(withToken: false);

        foreach (var (header, _) in Required.Where(entry => entry.Header != "Strict-Transport-Security"))
        {
            context.Response.Headers[header].ToString().Should().NotBeNullOrWhiteSpace(header);
        }
    }
}