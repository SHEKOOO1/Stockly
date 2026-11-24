using Microsoft.EntityFrameworkCore;
using Stockly.Domain.Common;

namespace Stockly.Infrastructure.Persistence;

/// <summary>
/// The ambient tenant for the current request.
/// <para>
/// Tenant scope is derived from the <strong>authenticated session only</strong> and
/// never from a header, a route value or a body field (AGENTS.md §4). This is the
/// single place the DbContext reads it from.
/// </para>
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant id, or <c>null</c> for an unauthenticated or platform-scope request.</summary>
    Guid? TenantId { get; }

    /// <summary>True only for a platform-authority session (ADR-0034).</summary>
    bool IsPlatformScope { get; }

    /// <summary>The membership id, or <c>null</c> outside a tenant session.</summary>
    Guid? MembershipId { get; }
}

/// <summary>
/// A fixed tenant for one unit of work — used by background jobs and by tests that
/// need to address a specific tenant explicitly rather than through the request scope.
/// </summary>
public sealed class FixedTenantContext : ITenantContext
{
    public Guid? TenantId { get; }

    public bool IsPlatformScope { get; }

    public Guid? MembershipId { get; }

    public FixedTenantContext(Guid? tenantId, Guid? membershipId = null, bool isPlatformScope = false)
    {
        TenantId = tenantId;
        MembershipId = membershipId;
        IsPlatformScope = isPlatformScope;
    }

    public static FixedTenantContext None { get; } = new(null);
}