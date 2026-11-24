using System.Net;

namespace Stockly.Api.Common;

/// <summary>
/// Resolves the caller's IP address for rate limiting and audit attribution.
/// <para>
/// <c>X-Forwarded-For</c> is attacker-controlled. Trusting it unconditionally would
/// hand every rate limiter in the system a free bypass: rotate a fake header per
/// request and each attempt looks like a new caller. S10
/// (<c>RateLimitBypassTests</c>) exists specifically to prove that cannot happen.
/// </para>
/// <para>
/// So the chain is walked right-to-left and the <strong>first address that is not a
/// trusted proxy</strong> is taken. Only when every hop is a trusted proxy does the
/// left-most entry win. An untrusted hop's injected address is never read.
/// </para>
/// <para>
/// This lives at the edge of the app rather than in the rate limiter, so there is
/// exactly one implementation of "what is the caller's IP" and it cannot drift
/// between call sites.
/// </para>
/// </summary>
public sealed class ForwardedIpResolver
{
    /// <summary>The standard proxy header.</summary>
    public const string ForwardedForHeader = "X-Forwarded-For";

    /// <summary>The standardised alternative, per RFC 7239.</summary>
    public const string ForwardedHeader = "Forwarded";

    /// <summary>The socket-level remote address, always present and unspoofable.</summary>
    public const string RemoteAddressHeader = "RemoteAddr";

    private readonly IReadOnlySet<string> _trustedProxies;

    /// <summary>Creates a resolver.</summary>
    /// <param name="trustedProxies">
    /// CIDR blocks or literal addresses of proxies whose added hops are trusted.
    /// Empty means no proxy is trusted, so the socket address is always used.
    /// </param>
    public ForwardedIpResolver(IEnumerable<string>? trustedProxies)
    {
        _trustedProxies = (trustedProxies ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A resolver that trusts nothing, so only the socket address is used.</summary>
    public static ForwardedIpResolver TrustNothing { get; } = new([]);

    /// <summary>
    /// The caller's address, or <see langword="null"/> when it cannot be established.
    /// </summary>
    public string? Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chain = ReadChain(context, out var hasVerifiedHop);

        // No socket peer means every remaining entry is caller-supplied, so none of
        // it may be used to identify the client.
        if (!hasVerifiedHop)
        {
            return null;
        }

        // Walk from the closest hop outwards; the first untrusted address is the
        // client. Anything further left was written by that client, not by a proxy.
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            if (!_trustedProxies.Contains(chain[i]))
            {
                return Normalise(chain[i]);
            }
        }

        // Every hop is a trusted proxy, so the left-most entry is the client.
        // Reaching here requires hasVerifiedHop, because otherwise we returned above.
        return chain.Count > 0 ? Normalise(chain[0]) : null;
    }

    /// <summary>
    /// Reads the hop chain, nearest-first. An entry that does not parse as an IP is
    /// dropped rather than passed on: a malformed value is not evidence.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="hasVerifiedHop">
    /// True when the chain is anchored on the socket peer, which is the only
    /// unspoofable element. Without it, the header entries are all caller-supplied
    /// and the resolver must not derive a client identity from them.
    /// </param>
    private IReadOnlyList<string> ReadChain(HttpContext context, out bool hasVerifiedHop)
    {
        var chain = new List<string>();
        hasVerifiedHop = false;

        // `Forwarded: for=192.0.2.1;for=198.51.100.2` (RFC 7239)
        if (context.Request.Headers.TryGetValue(ForwardedHeader, out var forwarded))
        {
            foreach (var header in forwarded)
            {
                if (header is null)
                {
                    continue;
                }

                foreach (var part in header.Split(','))
                {
                    foreach (var pair in part.Split(';'))
                    {
                        var separator = pair.IndexOf('=', StringComparison.Ordinal);
                        if (separator <= 0
                            || !pair.AsSpan(0, separator).Trim().Equals("for", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var value = pair[(separator + 1)..].Trim().Trim('"');

                        // Strip an RFC 7239 port suffix and IPv6 brackets.
                        if (value.StartsWith('['))
                        {
                            var close = value.IndexOf(']');
                            if (close > 0)
                            {
                                value = value[1..close];
                            }
                        }
                        else if (value.Count(c => c == ':') == 1)
                        {
                            value = value[..value.IndexOf(':', StringComparison.Ordinal)];
                        }

                        if (IPAddress.TryParse(value, out _))
                        {
                            chain.Add(value);
                        }
                    }
                }
            }
        }

        // `X-Forwarded-For: 203.0.113.7, 198.51.100.2` — read only as a fallback, and
        // only reached by the same trust check as above.
        if (context.Request.Headers.TryGetValue(ForwardedForHeader, out var xff))
        {
            foreach (var header in xff)
            {
                if (header is null)
                {
                    continue;
                }

                foreach (var part in header.Split(','))
                {
                    var value = part.Trim();
                    if (IPAddress.TryParse(value, out _))
                    {
                        chain.Add(value);
                    }
                }
            }
        }

        // The socket peer is the closest hop and is never attacker-controlled. It is
        // also the only element of the chain that can be trusted unconditionally.
        var remote = context.Connection.RemoteIpAddress?.ToString();
        if (remote is not null && IPAddress.TryParse(remote, out _))
        {
            chain.Add(remote);
            hasVerifiedHop = true;
        }

        return chain;
    }

    /// <summary>
    /// Renders an address the way it is stored in <c>audit.AuditLog.IpAddress</c>
    /// (nvarchar(45), IPv6-safe) and compared in the throttle ledger.
    /// </summary>
    private static string? Normalise(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || !IPAddress.TryParse(address, out var parsed))
        {
            return null;
        }

        if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            && parsed.IsIPv4MappedToIPv6)
        {
            // Collapse ::ffff:203.0.113.7 so the same client has one identity in the
            // throttle ledger regardless of which form arrived.
            parsed = parsed.MapToIPv4();
        }

        return parsed.ToString();
    }
}