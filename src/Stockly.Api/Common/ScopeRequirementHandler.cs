using Microsoft.AspNetCore.Authorization;
using Stockly.Application.Common.Errors;
using Stockly.Application.Security;

namespace Stockly.Api.Common;

/// <summary>
/// Fails a request unless its <c>scp</c> scope matches the route's scope.
/// <para>
/// This is the ASP.NET enforcement point for ADR-0034. The rule itself lives in
/// <see cref="RouteScopeResolver"/> as a pure function so it can be proven by unit
/// tests; this policy is only the bridge from a route to that function.
/// </para>
/// <para>
/// A mismatch is <c>403 permission_denied</c>, not <c>401</c>: the token is
/// genuine, it is simply not the right kind of authority. The distinction matters
/// because <c>401</c> would invite a client to re-authenticate in a loop, and the
/// body deliberately does not say which scope was required.
/// </para>
/// </summary>
public sealed class ScopeRequirementHandler : AuthorizationHandler<ScopeRequirement>
{
    /// <summary>
    /// Builds the requirement for the route being evaluated.
    /// <para>
    /// The scope comes from the request path, never from a claim or a header, so a
    /// caller cannot select its own scope by sending one.
    /// </para>
    /// </summary>
    public static ScopeRequirement ForRequest(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return new ScopeRequirement(RouteScopeResolver.Resolve(httpContext.Request.Path.Value));
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (requirement.Route == RouteScope.Anonymous)
        {
            // An anonymous route is not this policy's business; letting it through
            // here avoids accidentally tightening `/health` or the docs endpoints.
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var scope = TryBuildScope(context.User);
        if (scope is null)
        {
            // No `scp` claim, or an unreadable one. S25 requires rejection rather
            // than a fallback to the tenant scope.
            return Task.CompletedTask;
        }

        if (RouteScopeResolver.Allows(scope, requirement.Route))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rebuilds the request scope from the principal.
    /// <para>
    /// The tenant is read as <c>Guid</c> rather than as a raw claim string so a
    /// malformed <c>tid</c> yields <see langword="null"/> instead of propagating a
    /// value the query filter would then have to defend against.
    /// </para>
    /// </summary>
    internal static RequestScope? TryBuildScope(System.Security.Claims.ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!SessionScopeClaim.TryParse(user.FindFirst("scp")?.Value, out var scope))
        {
            // No `scp` claim, or one outside the documented `tenant`/`platform` set.
            // S25 requires rejection rather than a fallback to the tenant scope.
            return null;
        }

        var userId = user.FindFirst("sub")?.Value;
        var sessionId = user.FindFirst("sid")?.Value;

        // `sub` and `sid` are not optional in a validated token; without them there is
        // no auditable actor, so the request is not authorised.
        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(sessionId, out var sessionGuid))
        {
            return null;
        }

        return new RequestScope
        {
            UserId = userGuid,
            SessionId = sessionGuid,
            Scope = scope,
            TenantId = Guid.TryParse(user.FindFirst("tid")?.Value, out var tenantId) ? tenantId : null,
            MembershipId = Guid.TryParse(user.FindFirst("mid")?.Value, out var membershipId) ? membershipId : null,
            DeviceId = Guid.TryParse(user.FindFirst("did")?.Value, out var deviceId) ? deviceId : null,
        };
    }
}

/// <summary>
/// Requires an authenticated request whose <c>scp</c> matches a specific route
/// scope.
/// </summary>
public sealed class ScopeRequirement : IAuthorizationRequirement
{
    /// <summary>Creates the requirement.</summary>
    public ScopeRequirement(RouteScope route) => Route = route;

    /// <summary>The scope the route belongs to.</summary>
    public RouteScope Route { get; }

    /// <summary>
    /// The problem returned when the requirement is not met. Kept as a documented
    /// code so the failure is uniform across every route.
    /// </summary>
    public static BusinessErrorCode FailureCode => BusinessErrorCode.PermissionDenied;
}