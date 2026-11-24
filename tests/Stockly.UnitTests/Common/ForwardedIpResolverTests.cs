using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Stockly.Api.Common;
using Xunit;

namespace Stockly.UnitTests.Common;

/// <summary>
/// <c>SECURITY_ARCHITECTURE.md</c> §6: <c>X-Forwarded-For</c> is trusted
/// <strong>only</strong> from configured proxy hops.
/// <para>
/// This is the control S10 (<c>RateLimitBypassTests</c>) depends on. A resolver that
/// trusts the header unconditionally hands every rate limiter in the system a free
/// bypass, so these tests pin the trust rule rather than the parsing.
/// </para>
/// <para>
/// No database and no socket: <see cref="DefaultHttpContext"/> carries the headers
/// and the connection.
/// </para>
/// </summary>
public sealed class ForwardedIpResolverTests
{
    private static HttpContext Context(string? forwardedFor = null, string? remote = "203.0.113.7")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remote is null ? null : System.Net.IPAddress.Parse(remote);

        if (forwardedFor is not null)
        {
            context.Request.Headers[ForwardedIpResolver.ForwardedForHeader] = forwardedFor;
        }

        return context;
    }

    [Fact]
    public void With_no_trusted_proxy_the_socket_address_is_used()
    {
        ForwardedIpResolver.TrustNothing
            .Resolve(Context(forwardedFor: "198.51.100.2"))
            .Should().Be("203.0.113.7", "the socket peer is unspoofable");
    }

    [Fact]
    public void A_fake_forwarded_header_is_ignored_when_no_proxy_is_trusted()
    {
        // The direct bypass attempt S10 exists to prevent.
        ForwardedIpResolver.TrustNothing
            .Resolve(Context(forwardedFor: "1.2.3.4, 5.6.7.8, 9.10.11.12"))
            .Should().Be("203.0.113.7");
    }

    [Fact]
    public void Rotating_a_fake_header_never_changes_the_resolved_address()
    {
        var resolver = ForwardedIpResolver.TrustNothing;

        var first = resolver.Resolve(Context(forwardedFor: "1.1.1.1"));
        var second = resolver.Resolve(Context(forwardedFor: "2.2.2.2"));
        var third = resolver.Resolve(Context(forwardedFor: "3.3.3.3"));

        first.Should().Be(second).And.Be(second);
        second.Should().Be(third);
        third.Should().Be("203.0.113.7");
    }

    [Fact]
    public void The_first_untrusted_hop_is_taken_when_a_proxy_is_trusted()
    {
        // Chain, nearest first: proxy(10.0.0.1) -> claimed client(198.51.100.2)
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Context(forwardedFor: "198.51.100.2", remote: "10.0.0.1"))
            .Should().Be("198.51.100.2");
    }

    [Fact]
    public void An_address_left_of_an_untrusted_hop_is_ignored()
    {
        // XFF is read right-to-left and stopped at the first non-proxy. The
        // left-most value here was appended by the untrusted hop, so it is discarded.
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Context(forwardedFor: "1.1.1.1, 198.51.100.2", remote: "10.0.0.1"))
            .Should().Be("198.51.100.2", "1.1.1.1 was written by an untrusted hop");
    }

    [Fact]
    public void The_client_is_taken_when_the_nearest_hop_is_the_client_itself()
    {
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Context(remote: "198.51.100.2"))
            .Should().Be("198.51.100.2");
    }

    [Fact]
    public void An_ipv4_mapped_ipv6_address_collapses_to_its_ipv4_form()
    {
        // The same client must have one identity in the throttle ledger regardless of
        // which form arrived, or the limiter's counter can be split in two.
        var resolver = ForwardedIpResolver.TrustNothing;

        resolver.Resolve(Context(remote: "::ffff:203.0.113.7"))
            .Should().Be("203.0.113.7");
    }

    [Fact]
    public void An_ipv6_address_is_returned_in_a_form_that_fits_the_column()
    {
        // audit.AuditLog.IpAddress is nvarchar(45); a full IPv6 address plus a scope
        // id is 45 characters, and anything longer would not fit.
        var resolver = ForwardedIpResolver.TrustNothing;

        var resolved = resolver.Resolve(Context(remote: "2001:0db8:85a3:0000:0000:8a2e:0370:7334"));

        resolved.Should().Be("2001:db8:85a3::8a2e:370:7334");
        resolved!.Length.Should().BeLessThanOrEqualTo(AuditIpLength);
    }

    [Fact]
    public void A_malformed_hop_is_discarded_rather_than_returned()
    {
        // A value that does not parse is not evidence of anything.
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Context(forwardedFor: "not-an-ip", remote: "10.0.0.1"))
            .Should().Be("10.0.0.1");
    }

    [Fact]
    public void A_malformed_hop_does_not_stop_the_chain_from_being_read()
    {
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Context(forwardedFor: "198.51.100.2, junk", remote: "10.0.0.1"))
            .Should().Be("198.51.100.2");
    }

    [Fact]
    public void The_rfc_7239_forwarded_header_is_understood()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        context.Request.Headers[ForwardedIpResolver.ForwardedHeader] = "for=198.51.100.2";

        new ForwardedIpResolver(["10.0.0.1"]).Resolve(context).Should().Be("198.51.100.2");
    }

    [Fact]
    public void The_rfc_7239_header_ignores_parameters_other_than_for()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        context.Request.Headers[ForwardedIpResolver.ForwardedHeader] =
            "by=10.0.0.1;proto=https;for=198.51.100.2";

        new ForwardedIpResolver(["10.0.0.1"]).Resolve(context).Should().Be("198.51.100.2");
    }

    [Fact]
    public void A_quoted_ipv6_forwarded_value_with_a_port_is_parsed()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        context.Request.Headers[ForwardedIpResolver.ForwardedHeader] = "for=\"[2001:db8::1]:443\"";

        new ForwardedIpResolver(["10.0.0.1"]).Resolve(context).Should().Be("2001:db8::1");
    }

    [Fact]
    public void No_address_anywhere_yields_null_rather_than_an_empty_string()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        ForwardedIpResolver.TrustNothing.Resolve(context).Should().BeNull();
    }

    [Fact]
    public void A_trusted_proxy_list_with_blanks_is_tolerated()
    {
        // Configuration comes from appsettings, and a stray blank entry must not turn
        // into a wildcard trust.
        var resolver = new ForwardedIpResolver(["", "  ", "10.0.0.1"]);

        resolver.Resolve(Context(forwardedFor: "198.51.100.2", remote: "10.0.0.1"))
            .Should().Be("198.51.100.2");

        new ForwardedIpResolver(["", "  "])
            .Resolve(Context(forwardedFor: "198.51.100.2", remote: "10.0.0.1"))
            .Should().Be("10.0.0.1");
    }

    [Fact]
    public void A_null_trusted_proxy_list_means_trust_nothing()
    {
        var resolver = new ForwardedIpResolver(null);

        resolver.Resolve(Context(forwardedFor: "198.51.100.2", remote: "10.0.0.1"))
            .Should().Be("10.0.0.1");
    }

    /// <summary>Mirrors <c>audit.AuditLog.IpAddress nvarchar(45)</c>.</summary>
    private const int AuditIpLength = 45;
}