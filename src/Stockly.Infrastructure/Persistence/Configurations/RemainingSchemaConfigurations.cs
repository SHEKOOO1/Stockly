using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Audit;
using Stockly.Domain.Modules.Extensibility;
using Stockly.Domain.Modules.Files;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Inventory;
using Stockly.Domain.Modules.Notifications;
using Stockly.Domain.Modules.Ops;

namespace Stockly.Infrastructure.Persistence.Configurations;

// ---------------------------------------------------------------------------
// ops
// ---------------------------------------------------------------------------

internal sealed class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> b)
    {
        b.ToTable("DocumentSequence");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentType).IsRequired();
        b.Property(x => x.PeriodKey).HasMaxLength(20).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        b.Property(x => x.NextValue).IsRequired();
        b.Property(x => x.Padding).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // This index IS the concurrency guard. Allocation is
        //   UPDATE ... SET NextValue = NextValue + 1 WHERE ...
        // relying on the unique key to reject a colliding insert. Never MAX()+1.
        b.HasIndex(x => new { x.TenantId, x.DocumentType, x.PeriodKey })
            .IsUnique()
            .HasDatabaseName("UX_DocumentSequence_TenantId_DocumentType_PeriodKey");

        b.ToTable("DocumentSequence", t =>
            t.HasCheckConstraint("CK_DocumentSequence_Padding", "[Padding] BETWEEN 1 AND 18"));
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessage");
        b.HasKey(x => x.Id);
        b.Property(x => x.MessageType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.Attempts).IsRequired();
        b.Property(x => x.NextAttemptAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.LockedUntilUtc).HasPrecision(3);
        b.Property(x => x.CreatedAtUtc).HasPrecision(3);
        b.Property(x => x.ProcessedAtUtc).HasPrecision(3);
        b.Property(x => x.LastError).HasMaxLength(2000);

        // Deliberately no RowVersion: the dispatcher claims rows with a conditional
        // UPDATE (Status + LockedUntilUtc), and a concurrency token would let EF
        // silently discard a legitimate claim.

        // TenantId is nullable (platform announcements) and deliberately has NO
        // foreign key: a nullable composite FK to identity.Tenant would force the
        // principal key to be nullable, which SQL Server forbids on a unique index.

        b.HasIndex(x => new { x.Status, x.NextAttemptAtUtc }).HasDatabaseName("IX_OutboxMessage_Status_NextAttemptAtUtc");
        b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc }).HasDatabaseName("IX_OutboxMessage_TenantId_CreatedAtUtc");

        b.ToTable("OutboxMessage", t =>
        {
            t.HasCheckConstraint("CK_OutboxMessage_Status", "[Status] IN (1,2,3,4,5)");
            t.HasCheckConstraint("CK_OutboxMessage_Attempts", "[Attempts] >= 0");
        });
    }
}

// ---------------------------------------------------------------------------
// notifications
// ---------------------------------------------------------------------------

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notification");
        b.HasKey(x => x.Id);
        b.Property(x => x.Category).IsRequired();
        b.Property(x => x.Severity).Enum_().IsRequired();
        b.Property(x => x.Title).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.Body).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.ActionUrl).HasMaxLength(300);
        b.Property(x => x.EntityName).HasMaxLength(100);
        b.Property(x => x.IsRead).IsRequired();
        b.Property(x => x.ReadAtUtc).HasPrecision(3);
        b.Property(x => x.CreatedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.ExpiresAtUtc).HasPrecision(3);

        // No RowVersion (TenantRecordEntity): the ratified column list omits it.

        // Category stays an unconstrained smallint: the ratified design does not
        // enumerate its values and V1 has no lookup table for it (DATABASE_DESIGN §1.9).
        // Inventing a CHECK here would be an unapproved requirement decision.

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.MembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Notification_TenantMembership");

        b.HasIndex(x => new { x.TenantId, x.MembershipId, x.IsRead, x.CreatedAtUtc })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("IX_Notification_TenantId_MembershipId_IsRead_CreatedAtUtc");

        b.ToTable("Notification", t =>
            t.HasCheckConstraint("CK_Notification_Severity", "[Severity] IN (1,2,3)"));
    }
}

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        b.ToTable("NotificationPreference");
        b.HasKey(x => x.Id);
        b.Property(x => x.Category).IsRequired();
        b.Property(x => x.Channel).Enum_().IsRequired();
        b.Property(x => x.IsEnabled).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.MembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_NotificationPreference_TenantMembership");

        b.HasIndex(x => new { x.MembershipId, x.Category, x.Channel })
            .IsUnique()
            .HasDatabaseName("UX_NotificationPreference_MembershipId_Category_Channel");

        b.ToTable("NotificationPreference", t =>
            t.HasCheckConstraint("CK_NotificationPreference_Channel", "[Channel] IN (1,2,3)"));
    }
}

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> b)
    {
        b.ToTable("PushSubscription");
        b.HasKey(x => x.Id);
        b.Property(x => x.Endpoint).HasMaxLength(500).IsRequired();
        b.Property(x => x.P256dh).HasMaxLength(200);
        b.Property(x => x.Auth).HasMaxLength(200);
        b.Property(x => x.UserAgent).HasMaxLength(400);
        b.Property(x => x.ExpiresAtUtc).HasPrecision(3);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasPrecision(3).IsRequired();

        // No RowVersion (TenantRecordEntity).

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.MembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PushSubscription_TenantMembership");

        b.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PushSubscription_Device");

        // ADR-0032 exception: the endpoint is a globally unique server-issued URL,
        // not tenant business data.
        b.HasIndex(x => x.Endpoint)
            .IsUnique()
            .HasDatabaseName("UX_PushSubscription_Endpoint");
    }
}

// ---------------------------------------------------------------------------
// files
// ---------------------------------------------------------------------------

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.ToTable("Attachment");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        b.Property(x => x.SizeBytes).IsRequired();
        b.Property(x => x.Sha256).HasColumnType("binary(32)").IsRequired().IsFixedLength();
        b.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.UploadedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.OcrStatus).Enum_().IsRequired();
        b.Property(x => x.ExtractedText).HasColumnType("nvarchar(max)");
        b.Property(x => x.RowVersion).IsRowVersion();

        // The original file name is metadata only and is never used to build a path.
        // ExtractedText is reserved for V2 OCR (TBD-27) and is never written in V1.

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.UploadedByMembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Attachment_TenantMembership");

        // ADR-0032 exception: StorageKey is globally unique because
        // Append/GET/DELETE by key carry no tenant context.
        b.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasDatabaseName("UX_Attachment_StorageKey");

        b.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId })
            .HasDatabaseName("IX_Attachment_TenantId_EntityName_EntityId");

        b.ToTable("Attachment", t =>
        {
            t.HasCheckConstraint("CK_Attachment_SizeBytes", "[SizeBytes] >= 0");
            t.HasCheckConstraint("CK_Attachment_OcrStatus", "[OcrStatus] IN (0,1,2,3)");
        });
    }
}

// ---------------------------------------------------------------------------
// extensibility
// ---------------------------------------------------------------------------

internal sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> b)
    {
        b.ToTable("CustomFieldDefinition");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Label).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.LabelAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.FieldType).Enum_().IsRequired();
        b.Property(x => x.IsRequired).IsRequired();
        b.Property(x => x.IsFilterable).IsRequired();
        b.Property(x => x.Options).HasColumnType("nvarchar(max)");
        b.Property(x => x.DisplayOrder).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.EntityName, x.FieldKey })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_CustomFieldDefinition_TenantId_EntityName_FieldKey");

        b.ToTable("CustomFieldDefinition", t =>
            t.HasCheckConstraint("CK_CustomFieldDefinition_FieldType", "[FieldType] IN (1,2,3,4,5,6)"));
    }
}

internal sealed class CustomFieldValueConfiguration : IEntityTypeConfiguration<CustomFieldValue>
{
    public void Configure(EntityTypeBuilder<CustomFieldValue> b)
    {
        b.ToTable("CustomFieldValue");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.ValueJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<CustomFieldDefinition>()
            .WithMany()
            .HasForeignKey(x => new { x.CustomFieldDefinitionId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_CustomFieldValue_CustomFieldDefinition");

        b.HasIndex(x => new { x.CustomFieldDefinitionId, x.EntityName, x.EntityId })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldValue_CustomFieldDefinitionId_EntityName_EntityId");
    }
}

// ---------------------------------------------------------------------------
// audit
// ---------------------------------------------------------------------------

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLog");

        // The ONLY business table with a database-ordered identity: append-only
        // ordering must be total across transactions, which an application key
        // cannot guarantee (DATABASE_DESIGN §4.9).
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.OccurredAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.Category).Enum_().IsRequired();
        b.Property(x => x.Severity).Enum_().IsRequired();
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.Result).Enum_().IsRequired();
        b.Property(x => x.EntityName).HasMaxLength(100);
        b.Property(x => x.EntityId).HasMaxLength(100);
        b.Property(x => x.IpAddress).HasMaxLength(Convention.IpLength);
        b.Property(x => x.UserAgent).HasMaxLength(400);
        b.Property(x => x.CorrelationId).HasMaxLength(100);
        b.Property(x => x.ReasonCode).HasMaxLength(200);
        b.Property(x => x.Quantity).Money_();
        b.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.AfterJson).HasColumnType("nvarchar(max)");

        // No RowVersion, no update path, no delete path. BeforeJson/AfterJson are
        // redacted before they get here (AGENTS.md §4).
        //
        // TenantId, ActorMembershipId, SessionId, ProductId and UnitId carry NO
        // foreign key: the audit log must survive deletion of the row it describes,
        // and a dangling historical reference is the point.

        b.HasIndex(x => new { x.TenantId, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_AuditLog_TenantId_OccurredAtUtc");

        b.HasIndex(x => new { x.Category, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_AuditLog_Category_OccurredAtUtc");

        b.HasIndex(x => new { x.ActorUserId, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_AuditLog_ActorUserId_OccurredAtUtc");

        b.HasIndex(x => new { x.EntityName, x.EntityId, x.OccurredAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_AuditLog_EntityName_EntityId_OccurredAtUtc");

        b.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_AuditLog_CorrelationId");

        b.ToTable("AuditLog", t =>
        {
            t.HasCheckConstraint("CK_AuditLog_Category", "[Category] IN (1,2,3,4,5,6,7,8)");
            t.HasCheckConstraint("CK_AuditLog_Severity", "[Severity] IN (1,2,3)");
            t.HasCheckConstraint("CK_AuditLog_Result", "[Result] IN (1,2,3)");
        });
    }
}