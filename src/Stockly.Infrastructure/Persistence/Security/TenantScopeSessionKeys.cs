namespace Stockly.Infrastructure.Persistence.Security;

/// <summary>
/// The <c>SESSION_CONTEXT</c> keys that carry the authenticated database scope from the
/// application into SQL Server.
/// <para>
/// These are the <strong>only</strong> keys Stockly reads, and they are written by
/// <see cref="TenantScopeConnectionInterceptor"/> from the server-side
/// <see cref="ITenantContext"/>. Nothing reads a tenant id from a header, a route value
/// or a body field (AGENTS.md §4), and nothing derives a scope from the absence of a
/// tenant id.
/// </para>
/// </summary>
public static class TenantScopeSessionKeys
{
    /// <summary>
    /// The scope discriminator. Set to <see cref="TenantScopeValues.Tenant"/> or
    /// <see cref="TenantScopeValues.Platform"/>.
    /// </summary>
    public const string Scope = "StocklyScope";

    /// <summary>
    /// The authenticated tenant id, a <c>uniqueidentifier</c>. Meaningful only while
    /// <see cref="Scope"/> is <see cref="TenantScopeValues.Tenant"/>.
    /// </summary>
    public const string TenantId = "StocklyTenantId";
}

/// <summary>
/// The two recognised scope values. They mirror <c>SessionScope</c> and
/// <c>SessionScopeClaim</c>: there is deliberately no numeric or permissive third value,
/// and anything unrecognised denies (fail closed).
/// </summary>
public static class TenantScopeValues
{
    /// <summary>An ordinary tenant session. Requires <c>StocklyTenantId</c>.</summary>
    public const int Tenant = 1;

    /// <summary>
    /// A platform-authority session (ADR-0034). Authorised by membership of the
    /// <c>stockly_platform_role</c> database role, <em>not</em> by the value alone.
    /// </summary>
    public const int Platform = 2;
}

/// <summary>
/// The database role that confers platform authority.
/// <para>
/// It is a database role rather than a session-context value precisely so that setting
/// <c>StocklyScope = 2</c> is not sufficient on its own. A client cannot grant itself a
/// role membership: that requires a <c>ALTER ROLE</c> executed against the database by
/// the deployment principal. Verified on SQL Server: a session that set
/// <c>StocklyScope = 2</c> without membership still read zero rows.
/// </para>
/// </summary>
public static class TenantScopePlatformRole
{
    /// <summary>The role name, checked inside the predicate via <c>IS_MEMBER</c>.</summary>
    public const string Name = "stockly_platform_role";
}