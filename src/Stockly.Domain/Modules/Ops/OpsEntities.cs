using Stockly.Domain.Common;

namespace Stockly.Domain.Modules.Ops;

/// <summary>
/// ops.DocumentSequence (§4.3). UQ: <c>(TenantId, DocumentType, PeriodKey)</c>.
/// <para>
/// Number allocation is <c>UPDATE … SET NextValue = NextValue + 1</c> with that
/// unique index as the concurrency guard, so a failed transaction burns a number
/// rather than handing the same number to two documents. Never
/// <c>MAX(number)+1</c> in application code.
/// </para>
/// </summary>
public class DocumentSequence : TenantScopedEntity
{
    /// <summary>Stock document type / PO / GR / count.</summary>
    public short DocumentType { get; set; }

    /// <summary>e.g. <c>2026</c> for yearly numbering.</summary>
    public string PeriodKey { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public long NextValue { get; set; }

    /// <summary>smallint, default 6.</summary>
    public short Padding { get; set; } = 6;
}

/// <summary>
/// ops.OutboxMessage (§4.3). Written in the same transaction as the state change
/// it announces (AD-NUM-02), so a committed change can never lose its event and a
/// failed change can never emit one. <c>Payload</c> is nvarChar(max) — justified:
/// an opaque integration payload (DATABASE_DESIGN §1.1).
/// <para>
/// <c>TenantId</c> is nullable because platform-level announcements have no tenant.
/// </para>
/// </summary>
public class OutboxMessage : Entity
{
    public Guid? TenantId { get; set; }

    public string MessageType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public OutboxStatus Status { get; set; }

    public short Attempts { get; set; }

    public DateTime NextAttemptAtUtc { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public DateTime? CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>Sanitised — never a stack trace and never a client-visible detail.</summary>
    public string? LastError { get; set; }
}