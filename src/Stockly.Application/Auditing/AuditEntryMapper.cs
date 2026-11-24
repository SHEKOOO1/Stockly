using System.Globalization;
using System.Text.Json;
using Stockly.Domain.Common;
using Stockly.Domain.Modules.Audit;

namespace Stockly.Application.Auditing;

/// <summary>
/// The caller's identity and request context for one audited action.
/// <para>
/// Every field is optional because an audited action is not always attributable: a
/// failed login has no session, and a platform event has no tenant. The point is
/// that absence is <em>recorded as absence</em> rather than filled with a guess.
/// </para>
/// </summary>
public sealed record AuditActor
{
    /// <summary>No actor: a failed login, a rejected token, a scheduled job.</summary>
    public static AuditActor None { get; } = new();

    /// <summary>The <c>AppUser.Id</c> behind the action, when authenticated.</summary>
    public Guid? UserId { get; init; }

    /// <summary>The active <c>TenantMembership.Id</c>, for a tenant session.</summary>
    public Guid? MembershipId { get; init; }

    /// <summary>The tenant the action belongs to. Null for platform-level events.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>The bound device, for a shared terminal.</summary>
    public Guid? DeviceId { get; init; }

    /// <summary>The <c>AuthSession.Id</c> the request ran under.</summary>
    public Guid? SessionId { get; init; }

    /// <summary>
    /// The caller IP as resolved by the trusted proxy chain. <see langword="null"/>
    /// when it cannot be established — never a value taken from an untrusted header.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>The user agent, truncated.</summary>
    public string? UserAgent { get; init; }

    /// <summary>The request correlation id, shared with logs and the error body.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// True when the actor has no tenant. Used by the writer to enforce
    /// <c>TenantId</c> being non-null for a tenant-scoped category.
    /// </summary>
    public bool IsPlatformScope => TenantId is null;
}

/// <summary>One audited action, before it is turned into an <see cref="AuditLog"/> row.</summary>
public sealed record AuditEntry
{
    /// <summary>The category, which decides the <c>TenantId</c> rule and the severity floor.</summary>
    public required AuditCategory Category { get; init; }

    /// <summary>The action name, e.g. <c>auth.login.failed</c>.</summary>
    public required string Action { get; init; }

    /// <summary>Whether the action succeeded, failed, or was denied.</summary>
    public required AuditResult Result { get; init; }

    /// <summary>Severity. Defaults from the category.</summary>
    public AuditSeverity? Severity { get; init; }

    /// <summary>The affected entity's table or type name.</summary>
    public string? EntityName { get; init; }

    /// <summary>The affected entity's key. A string because not every entity key is a GUID.</summary>
    public string? EntityId { get; init; }

    /// <summary>A stable reason code, never a free-form sentence.</summary>
    public string? ReasonCode { get; init; }

    /// <summary>A product the action concerned.</summary>
    public Guid? ProductId { get; init; }

    /// <summary>A quantity the action concerned.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>A unit the action concerned.</summary>
    public Guid? UnitId { get; init; }

    /// <summary>The state before the change. Refused if it contains a secret.</summary>
    public object? Before { get; init; }

    /// <summary>The state after the change. Refused if it contains a secret.</summary>
    public object? After { get; init; }

    /// <summary>Who and from where the action came.</summary>
    public AuditActor Actor { get; init; } = AuditActor.None;

    /// <summary>When it happened. Defaults to now, in UTC.</summary>
    public DateTime? OccurredAtUtc { get; init; }
}

/// <summary>
/// Turns an <see cref="AuditEntry"/> into an <see cref="AuditLog"/> row.
/// <para>
/// This is a pure function: it never touches a <c>DbContext</c>. Rule 6 of the
/// audit model (the row is written inside the business transaction) is satisfied by
/// the <em>caller</em> adding the produced row to its own change set, not by this
/// type saving anything. That separation is what makes the mapping testable with no
/// database.
/// </para>
/// <para>
/// Every before/after payload passes <see cref="AuditSecretRedactor"/> first, so a
/// secret cannot reach the row by any path (rule 5).
/// </para>
/// </summary>
public static class AuditEntryMapper
{
    /// <summary>JSON options for the payload columns. camelCase, matching the API.</summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Constants matching the ratified column widths in <c>DATABASE_DESIGN.md</c> §4.9.</summary>
    public static class Limits
    {
        /// <summary><c>Action</c> is <c>nvarchar(100)</c>.</summary>
        public const int Action = 100;

        /// <summary><c>EntityName</c> is <c>nvarchar(100)</c>.</summary>
        public const int EntityName = 100;

        /// <summary><c>EntityId</c> is <c>nvarchar(100)</c>.</summary>
        public const int EntityId = 100;

        /// <summary><c>ReasonCode</c> is <c>nvarchar(50)</c>.</summary>
        public const int ReasonCode = 50;

        /// <summary><c>CorrelationId</c> is <c>nvarchar(100)</c>.</summary>
        public const int CorrelationId = 100;

        /// <summary><c>UserAgent</c> is <c>nvarchar(400)</c>.</summary>
        public const int UserAgent = 400;

        /// <summary><c>IpAddress</c> is <c>nvarchar(45)</c> — IPv6-safe.</summary>
        public const int IpAddress = 45;
    }

    /// <summary>
    /// Maps an entry to a row. The caller adds the row to its change set inside the
    /// same transaction as the business change.
    /// </summary>
    /// <exception cref="Stockly.Application.Common.Security.AuditPayloadSecretException">
    /// A before/after payload contained a secret.
    /// </exception>
    public static AuditLog ToRow(AuditEntry entry, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Action))
        {
            throw new ArgumentException("An audit action must be named.", nameof(entry));
        }

        // Rule 5. Refused, not redacted: a partially-redacted payload is easy to
        // get wrong silently, and a leaked credential in an append-only table cannot
        // be removed afterwards.
        Stockly.Application.Common.Security.AuditSecretRedactor.EnsureNoSecrets(entry.Before, nameof(entry.Before));
        Stockly.Application.Common.Security.AuditSecretRedactor.EnsureNoSecrets(entry.After, nameof(entry.After));

        return new AuditLog
        {
            OccurredAtUtc = entry.OccurredAtUtc ?? utcNow,
            TenantId = entry.Actor.TenantId,
            Category = entry.Category,
            Severity = entry.Severity ?? DefaultSeverity(entry.Category, entry.Result),
            Action = Clip(entry.Action, Limits.Action),
            Result = entry.Result,
            EntityName = Truncate(entry.EntityName, Limits.EntityName),
            EntityId = Truncate(entry.EntityId, Limits.EntityId),
            ActorUserId = entry.Actor.UserId,
            ActorMembershipId = entry.Actor.MembershipId,
            DeviceId = entry.Actor.DeviceId,
            SessionId = entry.Actor.SessionId,
            IpAddress = Truncate(entry.Actor.IpAddress, Limits.IpAddress),
            UserAgent = Truncate(entry.Actor.UserAgent, Limits.UserAgent),
            CorrelationId = Truncate(entry.Actor.CorrelationId, Limits.CorrelationId),
            ReasonCode = Truncate(entry.ReasonCode, Limits.ReasonCode),
            ProductId = entry.ProductId,
            Quantity = entry.Quantity,
            UnitId = entry.UnitId,
            BeforeJson = Serialise(entry.Before),
            AfterJson = Serialise(entry.After),
        };
    }

    /// <summary>
    /// The severity floor for a category, raised to <see cref="AuditSeverity.Critical"/>
    /// for a denied or failed security event.
    /// <para>
    /// A security event that did not succeed is the interesting one, so it cannot be
    /// filed as <see cref="AuditSeverity.Info"/> by accident.
    /// </para>
    /// </summary>
    public static AuditSeverity DefaultSeverity(AuditCategory category, AuditResult result)
    {
        var floor = category switch
        {
            AuditCategory.Security => AuditSeverity.Warning,
            AuditCategory.Identity => AuditSeverity.Info,
            AuditCategory.Inventory => AuditSeverity.Info,
            AuditCategory.Purchasing => AuditSeverity.Info,
            AuditCategory.Events => AuditSeverity.Info,
            AuditCategory.Food => AuditSeverity.Info,
            AuditCategory.Administration => AuditSeverity.Warning,
            AuditCategory.Platform => AuditSeverity.Warning,
            _ => AuditSeverity.Info,
        };

        var unsuccessful = result is not AuditResult.Success;
        return unsuccessful && floor < AuditSeverity.Warning ? AuditSeverity.Warning : floor;
    }

    private static string? Serialise(object? payload) =>
        payload is null ? null : JsonSerializer.Serialize(payload, SerializerOptions) ?? "null";

    /// <summary>
    /// Clips an over-long value to its column width.
    /// <para>
    /// Truncating beats throwing: an audit row that is refused for a long user agent
    /// would lose the record of the action, which is worse than a clipped one.
    /// </para>
    /// </summary>
    private static string? Truncate(string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;

    /// <summary>The non-nullable form, for a column the caller has already validated.</summary>
    private static string Clip(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}