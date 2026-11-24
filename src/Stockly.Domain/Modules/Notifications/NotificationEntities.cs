using Stockly.Domain.Common;

namespace Stockly.Domain.Modules.Notifications;

/// <summary>
/// notifications.Notification (§4.9). Deliberately has <strong>no
/// <c>RowVersion</c></strong>: the ratified column list omits it, and a user
/// notification is not a mutable business record. Base type is therefore
/// <see cref="TenantRecordEntity"/>.
/// <para>
/// <c>Category</c> is left as <c>smallint</c> because the ratified design does not
/// enumerate its values and there is no lookup table for an enum in V1
/// (DATABASE_DESIGN §1.9). Inventing the value set here would be an unapproved
/// requirement decision.
/// </para>
/// </summary>
public class Notification : TenantRecordEntity
{
    public Guid MembershipId { get; set; }

    public short Category { get; set; }

    public NotificationSeverity Severity { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? ActionUrl { get; set; }

    public string? EntityName { get; set; }

    public Guid? EntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

/// <summary>notifications.NotificationPreference (§4.9). UQ: <c>(MembershipId, Category, Channel)</c>.</summary>
public class NotificationPreference : TenantScopedEntity
{
    public Guid MembershipId { get; set; }

    public short Category { get; set; }

    public NotificationChannel Channel { get; set; }

    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// notifications.PushSubscription (§4.9). UQ: <c>(Endpoint)</c> — globally unique,
/// the ADR-0032 exception, because a push endpoint is a globally unique
/// server-issued URL rather than tenant business data. Keys are stored because the
/// push service requires them and are never exposed to another tenant (ADR-0031).
/// </summary>
public class PushSubscription : TenantRecordEntity
{
    public Guid MembershipId { get; set; }

    public Guid? DeviceId { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string? P256dh { get; set; }

    public string? Auth { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}