using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stockly.Application.Security;
using Stockly.Domain.Common;
using Stockly.Infrastructure.Persistence;

namespace Stockly.Api.Tenancy;

/// <summary>
/// Resolves the ambient tenant from the <em>authenticated</em> request only.
/// <para>
/// This type is the enforcement point for the rule that tenant scope comes from
/// the session and never from input. It reads
/// <see cref="HttpContext.User"/> claims exclusively: there is no header, query
/// string, route value or body field anywhere in this file that could set a
/// tenant, and no path by which a caller can nominate one.
/// </para>
/// <para>
/// It lives in the API project, not in Infrastructure, so that the data layer
/// never takes a dependency on ASP.NET Core. Background jobs and tests bind a
/// tenant explicitly through <see cref="FixedTenantContext"/> instead.
/// </para>
/// <para>
/// The claims it trusts (<c>scp</c>, <c>tid</c>, <c>mid</c>) are only as good as
/// the JWT validation that produced them, which is why §4.4 requires <c>scp</c>
/// to be re-checked against the live <c>AuthSession</c> on every request. The
/// session re-check itself belongs to Phase 2, when token issuance arrives.
/// Until then a request with no <c>scp</c> claim — or with an unauthenticated
/// principal, or with a scope outside the documented <c>tenant</c>/<c>platform</c>
/// set — resolves to <see langword="null"/> for the tenant, which the query filter
/// treats as "match nothing" rather than "match everything".
/// </para>
/// <para>
/// A platform-scope session deliberately yields <see langword="null"/> for
/// <see cref="TenantId"/>. Authority does not accumulate across scopes
/// (ADR-0034): a platform operator reading tenant data must present a tenant
/// session, which means authenticating again.
/// </para>
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    internal const string ScopeClaim = "scp";
    internal const string TenantClaim = "tid";
    internal const string MembershipClaim = "mid";

    private readonly IHttpContextAccessor _accessor;

    /// <summary>Creates the context accessor.</summary>
    public HttpTenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

    /// <inheritdoc />
    public Guid? TenantId => Scope == SessionScope.Tenant ? ReadClaim(TenantClaim) : null;

    /// <inheritdoc />
    public Guid? MembershipId => Scope == SessionScope.Tenant ? ReadClaim(MembershipClaim) : null;

    /// <inheritdoc />
    public bool IsPlatformScope => TryResolveScope(out var scope) && scope == SessionScope.Platform;

    /// <summary>
    /// The validated scope of the request, or <see langword="null"/> when there is
    /// no authenticated principal or the <c>scp</c> claim is missing or outside the
    /// documented set.
    /// <para>
    /// The three outcomes are kept distinct on purpose. Collapsing "invalid" into
    /// "tenant" would let a token whose scope could not be read bind a tenant
    /// anyway, which is the fail-open case S25 exists to rule out; collapsing it into
    /// "platform" would silently widen authority the other way.
    /// </para>
    /// </summary>
    internal SessionScope? Scope => TryResolveScope(out var scope) ? scope : null;

    /// <summary>
    /// The authenticated principal, or <see langword="null"/>.
    /// <para>
    /// A <see cref="ClaimsPrincipal"/> can carry claims without being
    /// <em>authenticated</em> — that is what a default, anonymous
    /// <see cref="ClaimsIdentity"/> is. Reading claims from one would mean honouring
    /// a scope that no authentication step ever vouched for. `AGENTS.md` §4 requires
    /// tenant scope to come from the *authenticated* session, so an unauthenticated
    /// principal is treated as no principal at all.
    /// </para>
    /// </summary>
    private ClaimsPrincipal? Principal
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true ? user : null;
        }
    }

    /// <summary>
    /// Reads <c>scp</c> and refuses anything unrecognised.
    /// </summary>
    private bool TryResolveScope(out SessionScope scope)
    {
        if (Principal is not null
            && SessionScopeClaim.TryParse(Principal.FindFirstValue(ScopeClaim), out scope))
        {
            return true;
        }

        scope = SessionScope.Tenant;
        return false;
    }

    private Guid? ReadClaim(string claimType)
    {
        var raw = Principal?.FindFirstValue(claimType);
        return Guid.TryParse(raw, out var value) ? value : null;
    }
}