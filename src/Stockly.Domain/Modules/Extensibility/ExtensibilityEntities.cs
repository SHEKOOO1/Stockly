using Stockly.Domain.Common;

namespace Stockly.Domain.Modules.Extensibility;

/// <summary>extensibility.CustomFieldDefinition (§4.9). UQ (filtered) <c>(TenantId, EntityName, FieldKey) WHERE IsDeleted = 0</c>.</summary>
public class CustomFieldDefinition : TenantScopedSoftDeleteEntity
{
    public string EntityName { get; set; } = string.Empty;

    public string FieldKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? LabelAr { get; set; }

    public CustomFieldType FieldType { get; set; }

    public bool IsRequired { get; set; }

    public bool IsFilterable { get; set; }

    /// <summary>JSON — justified: dropdown options are user-defined (DATABASE_DESIGN §1.1).</summary>
    public string? Options { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// extensibility.CustomFieldValue (§4.9). UQ:
/// <c>(CustomFieldDefinitionId, EntityName, EntityId)</c>.
/// <c>ValueJson</c> is nvarChar(max) — justified: the user-defined field type is
/// unknown at design time.
/// </summary>
public class CustomFieldValue : TenantScopedEntity
{
    public Guid CustomFieldDefinitionId { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string ValueJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}