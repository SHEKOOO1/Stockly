using Stockly.Domain.Common;

namespace Stockly.Domain.Modules.Audit;

/// <summary>
/// audit.AuditLog (§4.9) — the tamper-evident security and business audit log
/// (SECURITY_ARCHITECTURE.md §9).
/// <para>
/// It is the only business table that uses a database-ordered identity
/// (<c>bigint identity</c>): append-only ordering must be total across
/// transactions, which an application-generated key cannot guarantee.
/// </para>
/// <para>
/// <strong>No <c>RowVersion</c>, no update path, no delete path.</strong> The
/// application principal holds <c>INSERT</c> and <c>SELECT</c> only.
/// </para>
/// <para>
/// <c>TenantId</c> is NULL for platform-level events, and <c>EntityId</c> is
/// nvarChar(100) rather than a GUID because some entities use non-GUID keys.
/// </para>
/// <para>
/// <c>BeforeJson</c>/<c>AfterJson</c> are redacted before they get here: the writer
/// refuses any payload containing a password, PIN, PIN hash, JWT, refresh token,
/// encryption key or connection string rather than relying on review
/// (AGENTS.md §4).
/// </para>
/// </summary>
public class AuditLog : IGlobalEntity
{
    /// <summary>bigint identity — high volume.</summary>
    public long Id { get; set; }

    /// <summary>Monthly partition key.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>NULL for platform-level events.</summary>
    public Guid? TenantId { get; set; }

    public AuditCategory Category { get; set; }

    public AuditSeverity Severity { get; set; }

    /// <summary>e.g. <c>stock.out.post</c>.</summary>
    public string Action { get; set; } = string.Empty;

    public AuditResult Result { get; set; }

    public string? EntityName { get; set; }

    /// <summary>A string, because some entities use non-GUID keys.</summary>
    public string? EntityId { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? ActorMembershipId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? SessionId { get; set; }

    /// <summary>nvarChar(45) — IPv6-safe.</summary>
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }

    public string? ReasonCode { get; set; }

    public Guid? ProductId { get; set; }

    public decimal? Quantity { get; set; }

    public Guid? UnitId { get; set; }

    /// <summary>Redacted.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>Redacted.</summary>
    public string? AfterJson { get; set; }
}