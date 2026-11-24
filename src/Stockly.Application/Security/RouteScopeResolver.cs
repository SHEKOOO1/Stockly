using Stockly.Application.Common.Errors;
using Stockly.Domain.Common;

namespace Stockly.Application.Security;

/// <summary>
/// Which authority a route belongs to. Derived from the path alone, never from a
/// claim or a header, so a caller cannot select its own scope.
/// </summary>
public enum RouteScope
{
    /// <summary>Unauthenticated or outside <c>/api/v1</c>: auth, health, docs.</summary>
    Anonymous = 0,

    /// <summary>A tenant route: <c>/api/v1/**</c> that is not under <c>/platform/</c>.</summary>
    Tenant = 1,

    /// <summary>A platform-operator route: <c>/api/v1/platform/**</c>.</summary>
    Platform = 2,
}

/// <summary>
/// The immutable authenticated context for one request.
/// <para>
/// This is the <strong>only</strong> source of tenant scope. It is built from a
/// validated token <em>and</em> a cross-check against the live
/// <c>AuthSession</c> row (§4.4: "<c>scp</c> is validated against the live
/// <c>AuthSession</c> on every request; it is never trusted on its own"). It is a
/// sealed record with no settable state, so no later code can re-point a request
/// at another tenant.
/// </para>
/// <para>
/// <see cref="TenantId"/> and <see cref="MembershipId"/> are nullable here because
/// a platform session has neither. <see cref="IsTenant"/> and
/// <see cref="IsPlatform"/> are the only supported way to read them, so a caller
/// cannot forget the null check.
/// </para>
/// </summary>
public sealed record RequestScope
{
    /// <summary>The <c>sub</c> claim — an <c>AppUser.Id</c>.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The <c>sid</c> claim — an <c>AuthSession.Id</c>.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>The <c>scp</c> claim, cross-checked against the live session row.</summary>
    public required SessionScope Scope { get; init; }

    /// <summary>The <c>tid</c> claim. <see langword="null"/> for a platform scope.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>The <c>mid</c> claim. <see langword="null"/> for a platform scope.</summary>
    public Guid? MembershipId { get; init; }

    /// <summary>The <c>did</c> claim. Absent for a non-device session.</summary>
    public Guid? DeviceId { get; init; }

    /// <summary>True only for a validated <c>scp=tenant</c> scope.</summary>
    public bool IsTenant => Scope == SessionScope.Tenant;

    /// <summary>True only for a validated <c>scp=platform</c> scope.</summary>
    public bool IsPlatform => Scope == SessionScope.Platform;

    /// <summary>
    /// The tenant scope for this request, or a thrown failure if there is none.
    /// This is the method a warehouse-scoped service should call, so that
    /// "forgot to check the scope" is a compile error rather than a review item.
    /// </summary>
    /// <exception cref="BusinessException">
    /// The request is not tenant-scoped. Callers on the tenant chain must not run
    /// without a tenant.
    /// </exception>
    public Guid RequireTenantId() => TenantId
        ?? throw new BusinessException(
            BusinessErrorCode.PermissionDenied,
            "This operation requires a tenant-scoped session.");
}

/// <summary>
/// Resolves a request path to its <see cref="RouteScope"/> and decides whether a
/// <see cref="RequestScope"/> may reach it.
/// <para>
/// This is the ADR-0034 rule, and it is a pure function so it can be proven by
/// unit tests with no database and no HTTP pipeline:
/// </para>
/// <list type="bullet">
/// <item>a <c>scp=platform</c> token is rejected by <strong>every</strong> tenant
/// route, <em>even when the same user holds an active tenant membership</em> —
/// authority does not accumulate across scopes;</item>
/// <item>a <c>scp=tenant</c> token is rejected inside <c>/api/v1/platform/**</c>;</item>
/// <item>a missing or unrecognised <c>scp</c> is rejected.</item>
/// </list>
/// </summary>
public static class RouteScopeResolver
{
    private const string ApiPrefix = "/api/v1";
    private const string PlatformPrefix = "/api/v1/platform";

    /// <summary>
    /// Classifies a request path. Comparison is ordinal and case-insensitive on
    /// the route prefix only, matching ASP.NET routing.
    /// </summary>
    /// <param name="path">The request path, e.g. <c>/api/v1/inventory/balances</c>.</param>
    public static RouteScope Resolve(string? path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return RouteScope.Anonymous;
        }

        if (path.Length > PlatformPrefix.Length
            && path[PlatformPrefix.Length] == '/'
            && path.StartsWith(PlatformPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return RouteScope.Platform;
        }

        return RouteScope.Tenant;
    }

    /// <summary>
    /// Decides whether an authenticated scope may call a route.
    /// </summary>
    /// <param name="scope">The validated request scope.</param>
    /// <param name="route">The classified route.</param>
    /// <returns>
    /// <see langword="true"/> when the scope matches the route. <see langword="false"/>
    /// for a mismatch, which the pipeline answers with
    /// <c>403 permission_denied</c> rather than <c>401</c>: the token is genuine,
    /// it is simply not the right kind of authority.
    /// </returns>
    public static bool Allows(RequestScope scope, RouteScope route) => route switch
    {
        RouteScope.Anonymous => false,
        RouteScope.Tenant => scope.IsTenant,
        RouteScope.Platform => scope.IsPlatform,
        _ => false,
    };
}