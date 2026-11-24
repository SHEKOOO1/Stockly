using Stockly.Domain.Common;

namespace Stockly.Application.Security;

/// <summary>
/// The one place a <c>scp</c> claim value becomes a <see cref="SessionScope"/>.
/// <para>
/// <c>API_CONVENTIONS.md</c> and <c>SECURITY_ARCHITECTURE.md</c> §4.4 both define the
/// claim as exactly <c>tenant</c> or <c>platform</c>, lowercase. That is the wire
/// value, and it is deliberately <strong>not</strong> <see cref="SessionScope"/>'s
/// member name: <c>SessionScope.Platform.ToString()</c> is <c>"Platform"</c>, and
/// comparing a claim against it silently treats every real platform token as a
/// tenant token.
/// </para>
/// <para>
/// Comparison is case-insensitive because <c>scp</c> is not a secret and a casing
/// mismatch must fail towards a decision the operator can see in the token, not
/// towards a silently downgraded session. It is a closed set: anything else is
/// refused, so a future enum member cannot start being honoured by accident.
/// </para>
/// <para>
/// <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)}"/> is deliberately not
/// used. It accepts numeric text, so <c>scp="2"</c> would parse as
/// <see cref="SessionScope.Platform"/> and hand platform authority to a token that
/// was never issued as one.
/// </para>
/// </summary>
public static class SessionScopeClaim
{
    /// <summary>The documented <c>scp</c> value for a tenant session.</summary>
    public const string Tenant = "tenant";

    /// <summary>The documented <c>scp</c> value for a platform session.</summary>
    public const string Platform = "platform";

    /// <summary>
    /// Reads a validated <c>scp</c> claim. Returns <see langword="false"/> for a
    /// missing, blank or unrecognised value, which the caller must treat as a
    /// rejection rather than as a default.
    /// </summary>
    public static bool TryParse(string? claim, out SessionScope scope)
    {
        scope = SessionScope.Tenant;

        if (string.IsNullOrWhiteSpace(claim))
        {
            return false;
        }

        var trimmed = claim.Trim();

        if (string.Equals(trimmed, Platform, StringComparison.OrdinalIgnoreCase))
        {
            scope = SessionScope.Platform;
            return true;
        }

        if (string.Equals(trimmed, Tenant, StringComparison.OrdinalIgnoreCase))
        {
            scope = SessionScope.Tenant;
            return true;
        }

        return false;
    }

    /// <summary>The canonical wire value for a scope, used when a token is issued.</summary>
    public static string ToClaimValue(SessionScope scope) => scope switch
    {
        SessionScope.Tenant => Tenant,
        SessionScope.Platform => Platform,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown session scope."),
    };
}