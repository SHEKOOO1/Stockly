using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Stockly.Api.Common;
using Xunit;

namespace Stockly.UnitTests.Common;

/// <summary>
/// Regression suite for the fourth defect found on 2026-10-01:
/// <c>X-Forwarded-For</c> was trusted unconditionally.
/// <para>
/// <c>SECURITY_ARCHITECTURE.md</c> §6 requires that the header be trusted
/// <strong>only</strong> from configured proxy hops, because it is entirely
/// attacker-controlled. <c>TESTING_STRATEGY.md</c> names the guard
/// <c>S10 RateLimitBypassTests</c>. The rate limiter itself is Phase 2, so these
/// tests do not pretend to test one: they test the property the limiter will depend
/// on, by <em>simulating</em> a limiter keyed on
/// <see cref="ForwardedIpResolver"/> and proving that no header a caller controls can
/// move the key.
/// </para>
/// <para>
/// Simulating rather than omitting matters. Writing "the limiter must not trust
/// XFF" as a comment leaves the property unverified until Phase 2, which is exactly
/// when it is forgotten. Here it fails now.
/// </para>
/// </summary>
public sealed class RateLimitBypassTests
{
    /// <summary>
    /// A minimal stand-in for the Phase 2 limiter: a fixed window keyed on the
    /// resolved client address, the way §6 says the real one will be keyed.
    /// </summary>
    private sealed class SimulatedFixedWindowLimiter(int limit)
    {
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

        public bool TryAcquire(string key)
        {
            _counts.TryGetValue(key, out var used);
            if (used >= limit)
            {
                return false;
            }

            _counts[key] = used + 1;
            return true;
        }

        public int AttemptsAllowed(HttpContext context, ForwardedIpResolver resolver, int attempts = 10)
        {
            var allowed = 0;
            for (var i = 0; i < attempts; i++)
            {
                var key = resolver.Resolve(context) ?? "unknown";
                if (TryAcquire(key))
                {
                    allowed++;
                }
            }

            return allowed;
        }
    }

    private static HttpContext Request(
        string? forwardedFor = null,
        string? forwarded = null,
        string remote = "203.0.113.7")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remote);

        if (forwardedFor is not null)
        {
            context.Request.Headers[ForwardedIpResolver.ForwardedForHeader] = forwardedFor;
        }

        if (forwarded is not null)
        {
            context.Request.Headers[ForwardedIpResolver.ForwardedHeader] = forwarded;
        }

        return context;
    }

    private static ForwardedIpResolver Direct() => ForwardedIpResolver.TrustNothing;

    // ---- The bypass itself ---------------------------------------------

    [Fact]
    public void Rotating_a_forged_header_cannot_buy_more_attempts()
    {
        // The attack: a caller who knows the limiter keys on client IP sends a
        // different X-Forwarded-For value on every request, so every attempt looks
        // like a brand-new client.
        const int limit = 5;
        var limiter = new SimulatedFixedWindowLimiter(limit);
        var forged = new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3", "4.4.4.4", "5.5.5.5", "6.6.6.6", "7.7.7.7" };

        var allowed = 0;
        foreach (var header in forged)
        {
            var context = Request(forwardedFor: header);
            if (limiter.TryAcquire(Direct().Resolve(context) ?? "unknown"))
            {
                allowed++;
            }
        }

        allowed.Should().Be(limit, "the forged header must not create a new limiter key");
    }

    [Fact]
    public void A_repeated_forged_header_does_not_reset_the_window_either()
    {
        var limiter = new SimulatedFixedWindowLimiter(3);
        var resolver = Direct();
        var context = Request(forwardedFor: "1.1.1.1");

        limiter.AttemptsAllowed(context, resolver).Should().Be(3);
        limiter.AttemptsAllowed(context, resolver).Should().Be(0, "the window is still exhausted");
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("1.1.1.1, 2.2.2.2, 3.3.3.3, 4.4.4.4")]
    [InlineData("999.999.999.999")]
    [InlineData("not-an-ip")]
    [InlineData("")]
    [InlineData("unknown")]
    public void No_forged_header_value_changes_the_limiter_key(string forged)
    {
        var resolver = Direct();

        resolver.Resolve(Request(forwardedFor: forged))
            .Should().Be(resolver.Resolve(Request()), "the socket address is the only trusted source");
    }

    [Fact]
    public void A_forged_rfc_7239_header_is_also_ignored_when_no_proxy_is_trusted()
    {
        var resolver = Direct();

        resolver.Resolve(Request(forwarded: "for=1.1.1.1"))
            .Should().Be(resolver.Resolve(Request()));
    }

    [Fact]
    public void The_two_forwarded_headers_cannot_be_combined_to_forge_a_key()
    {
        // A caller who knows the app reads both will try one trusted-looking header
        // and one standard one.
        var resolver = Direct();

        resolver.Resolve(Request(forwardedFor: "1.1.1.1", forwarded: "for=2.2.2.2"))
            .Should().Be(resolver.Resolve(Request()));
    }

    // ---- Legitimate proxy behaviour must still work ----------------------

    [Fact]
    public void A_configured_proxy_may_still_report_the_real_client()
    {
        // The counter-test. Refusing every proxy would also "pass" every bypass test
        // while breaking every real deployment behind Nginx.
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Request(forwardedFor: "198.51.100.2", remote: "10.0.0.1"))
            .Should().Be("198.51.100.2");
    }

    [Fact]
    public void A_client_behind_a_configured_proxy_gets_its_own_limiter_window()
    {
        var limiter = new SimulatedFixedWindowLimiter(3);
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        limiter.AttemptsAllowed(Request(forwardedFor: "198.51.100.2", remote: "10.0.0.1"), resolver)
            .Should().Be(3);

        limiter.AttemptsAllowed(Request(forwardedFor: "198.51.100.9", remote: "10.0.0.1"), resolver)
            .Should().Be(3, "a genuinely different client gets its own window");
    }

    [Fact]
    public void One_client_cannot_smuggle_a_second_identity_past_a_configured_proxy()
    {
        // The chain says the client is 198.51.100.2; the left-most entry is the
        // forged one and must be discarded.
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);
        var limiter = new SimulatedFixedWindowLimiter(3);

        limiter.AttemptsAllowed(Request(forwardedFor: "1.1.1.1, 198.51.100.2", remote: "10.0.0.1"), resolver)
            .Should().Be(3);

        // Same real client, forged prefix again: still the same exhausted window.
        limiter.AttemptsAllowed(Request(forwardedFor: "2.2.2.2, 198.51.100.2", remote: "10.0.0.1"), resolver)
            .Should().Be(0);
    }

    [Fact]
    public void An_untrusted_hop_in_the_chain_stops_the_walk()
    {
        // A client that is not behind the configured proxy cannot inject anything,
        // because the walk stops at the first non-proxy.
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        resolver.Resolve(Request(forwardedFor: "1.1.1.1, 2.2.2.2", remote: "198.51.100.2"))
            .Should().Be("198.51.100.2");
    }

    // ---- Key stability, which the ledger depends on ----------------------

    [Fact]
    public void The_same_client_always_produces_the_same_key()
    {
        var resolver = new ForwardedIpResolver(["10.0.0.1"]);

        var keys = Enumerable.Range(0, 20)
            .Select(_ => resolver.Resolve(Request(forwardedFor: "198.51.100.2", remote: "10.0.0.1")))
            .Distinct()
            .ToArray();

        keys.Should().ContainSingle();
    }

    [Fact]
    public void An_ipv4_and_ipv4_mapped_ipv6_form_share_one_key()
    {
        // Otherwise a client could double its allowance by switching transport form.
        var resolver = ForwardedIpResolver.TrustNothing;

        var viaV4 = resolver.Resolve(Request(remote: "203.0.113.7"));
        var viaV6 = resolver.Resolve(Request(remote: "::ffff:203.0.113.7"));

        viaV6.Should().Be(viaV4);
    }

    [Fact]
    public void A_client_with_no_address_still_gets_a_stable_key()
    {
        // With no socket address and no trusted proxy anchor, header values are
        // entirely caller-supplied: returning null is the only safe answer. The
        // Phase 2 rate limiter must treat this as a non-trusted request (reject or
        // apply its strictest key) rather than deriving the key from the header.
        var limiter = new SimulatedFixedWindowLimiter(2);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers[ForwardedIpResolver.ForwardedForHeader] = "1.1.1.1";

        Direct().Resolve(context).Should().BeNull();

        var allowed = 0;
        for (var i = 0; i < 5; i++)
        {
            if (limiter.TryAcquire(Direct().Resolve(context) ?? "unknown"))
            {
                allowed++;
            }
        }

        allowed.Should().Be(2);
    }
}