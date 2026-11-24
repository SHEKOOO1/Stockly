using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Master;

namespace Stockly.Domain.Modules.Events;

/// <summary>
/// events.Event (§4.7). UQ (filtered) <c>(TenantId, Code) WHERE IsDeleted = 0</c>;
/// CHECK: <c>EndAtUtc &gt; StartAtUtc</c>.
/// <para>
/// There is deliberately <strong>no <c>CostSnapshotId</c></strong> (ADR-0037): the
/// snapshot row is written <em>after</em> the event is costed, so a foreign key
/// would be permanently null or force an impossible write order. "Current" is the
/// newest <c>food.EventFoodCostSnapshot</c> for the event.
/// </para>
/// </summary>
public class Event : TenantScopedSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public EventType Type { get; set; }

    /// <summary>Primary operating warehouse.</summary>
    public Guid WarehouseId { get; set; }

    public DateTime StartAtUtc { get; set; }

    public DateTime EndAtUtc { get; set; }

    /// <summary>CHECK &gt; 0; maintained from the dates.</summary>
    public short DurationDays { get; set; }

    public int ExpectedAttendees { get; set; }

    public EventStatus Status { get; set; }

    public string? MealPlanNotes { get; set; }

    public decimal? BudgetAmount { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<EventRequirement> Requirements { get; set; } = [];

    public ICollection<EventConsumption> Consumptions { get; set; } = [];

    public ICollection<EventAttendance> Attendances { get; set; } = [];
}

/// <summary>
/// events.EventRequirement (§4.7) — a "how much do we need" rule.
/// CHECK: <c>BasisType = 1 OR QuantityPerBasisUnit IS NOT NULL</c>.
/// Two uniques (ADR-0041): <c>(TenantId, EventId, ProductId, MealTypeId)</c> and
/// <c>(TenantId, EventId, ProductId) WHERE MealTypeId IS NULL</c>.
/// </summary>
public class EventRequirement : TenantScopedEntity
{
    public Guid EventId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>NULL = an event-wide requirement rather than a per-meal one.</summary>
    public Guid? MealTypeId { get; set; }

    public RequirementBasisType BasisType { get; set; }

    /// <summary>Required when <see cref="BasisType"/> is not Total.</summary>
    public decimal? QuantityPerBasisUnit { get; set; }

    public decimal TotalRequiredQuantity { get; set; }

    public Guid UnitId { get; set; }

    public bool IsCritical { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// events.EventConsumption (§4.7). CHECK: <c>ConsumedQuantity &gt;= 0</c>.
/// <para>
/// There is deliberately <strong>no <c>CHECK (ConsumedQuantity &lt;= PlannedQuantity * 2)</c></strong>
/// (ADR-0039): actual consumption legitimately exceeds a plan — that is precisely
/// the signal a manager needs — so the sanity guard is an application rule with a
/// configurable multiplier, an override permission and an audit record.
/// </para>
/// </summary>
public class EventConsumption : TenantScopedEntity
{
    public Guid EventId { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? MealTypeId { get; set; }

    public Guid? BatchId { get; set; }

    public decimal PlannedQuantity { get; set; }

    public decimal ConsumedQuantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? ActualUnitCost { get; set; }

    /// <summary>Proof of actual consumption.</summary>
    public Guid? StockDocumentId { get; set; }
}

/// <summary>
/// events.EventAttendance (§4.7). CHECK: <c>DaysAttending &gt; 0</c>,
/// <c>MealsPerDay IS NULL OR MealsPerDay &gt;= 0</c>.
/// <para>
/// V1 simplification (TBD-10): the daily counters live on the attendance row and
/// there are <strong>no child rows</strong>. A per-meal-type attendance matrix is
/// V1.1 and stays inside TBD-10.
/// </para>
/// </summary>
public class EventAttendance : TenantScopedEntity
{
    public Guid EventId { get; set; }

    /// <summary>NULL for a walk-in guest with no Stockly account.</summary>
    public Guid? MembershipId { get; set; }

    public string? GuestName { get; set; }

    public bool IsAttending { get; set; }

    public short DaysAttending { get; set; } = 1;

    /// <summary>Whole/half meals per day (TBD-10). NULL = not planned.</summary>
    public decimal? MealsPerDay { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// events.EventShortageSnapshot (§4.7). Append-only (ADR-0037): no
/// <c>RowVersion</c>, no <c>IsDeleted</c>, no update. The current shortage for an
/// event/product is the newest row. Used for shortage-trend reporting.
/// </summary>
public class EventShortageSnapshot : AppendOnlyTenantScopedEntity
{
    public Guid EventId { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid ProductId { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public decimal RequiredQuantity { get; set; }

    public decimal OnHandQuantity { get; set; }

    public decimal IncomingQuantity { get; set; }

    public decimal ShortageQuantity { get; set; }

    public decimal RecommendedPurchaseQuantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal? EstimatedCost { get; set; }

    public Guid? CreatedByMembershipId { get; set; }
}