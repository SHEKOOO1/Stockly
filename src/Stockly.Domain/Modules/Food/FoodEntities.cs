using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Master;

namespace Stockly.Domain.Modules.Food;

/// <summary>food.MealType (§4.8). UQ (filtered) <c>(TenantId, Code) WHERE IsDeleted = 0</c>.</summary>
public class MealType : TenantScopedSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    /// <summary>decimal(5,2).</summary>
    public decimal? DefaultServingsPerDay { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// food.Recipe (§4.8). UQ (filtered) <c>(TenantId, Code) WHERE IsDeleted = 0</c>;
/// CHECK: <c>YieldQuantity &gt; 0</c>.
/// </summary>
public class Recipe : TenantScopedSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public Guid MealTypeId { get; set; }

    /// <summary>CHECK &gt; 0.</summary>
    public decimal YieldQuantity { get; set; }

    public Guid YieldUnitId { get; set; }

    /// <summary>decimal(9,2). A nullable owner-supplied value (TBD-10), not a design TBD.</summary>
    public decimal? ServingsPerYield { get; set; }

    public int? PreparationMinutes { get; set; }

    public int? CookingMinutes { get; set; }

    /// <summary>smallint. No lookup table exists in V1, so this stays an opaque smallint (DATABASE_DESIGN §1.9).</summary>
    public short? Difficulty { get; set; }

    public string? Instructions { get; set; }

    public Guid? ImageAttachmentId { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RecipeItem> Items { get; set; } = [];
}

/// <summary>
/// food.RecipeItem (§4.8). UQ: <c>(RecipeId, LineNumber)</c>. CHECK:
/// <c>QuantityPerYield &gt; 0</c>, <c>WastagePercent BETWEEN 0 AND 100</c>.
/// </summary>
public class RecipeItem : TenantScopedEntity
{
    public Guid RecipeId { get; set; }

    public short LineNumber { get; set; }

    public Guid ProductId { get; set; }

    public decimal QuantityPerYield { get; set; }

    public Guid UnitId { get; set; }

    /// <summary>decimal(5,2), default 0.</summary>
    public decimal WastagePercent { get; set; }

    public bool IsOptional { get; set; }

    public string? Notes { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>
/// food.RecipeCostSnapshot (§4.8). Append-only (ADR-0037): no <c>RowVersion</c>, no
/// update, no delete. The current recipe cost is the newest row.
/// <c>LineCosts</c> is nvarChar(max) — a point-in-time read snapshot, not a source
/// of truth (DATABASE_DESIGN §1.1).
/// </summary>
public class RecipeCostSnapshot : AppendOnlyTenantScopedEntity
{
    public Guid RecipeId { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public decimal TotalCost { get; set; }

    public decimal YieldQuantity { get; set; }

    public Guid YieldUnitId { get; set; }

    public decimal CostPerYieldUnit { get; set; }

    public decimal? ServingsPerYield { get; set; }

    public decimal? CostPerServing { get; set; }

    /// <summary>Human-readable provenance, e.g. <c>3 estimated, 5 actual</c>.</summary>
    public string? CostSourceSummary { get; set; }

    public ValuationMethod ValuationMethod { get; set; }

    /// <summary>JSON per-ingredient breakdown — a read snapshot, not a source of truth.</summary>
    public string? LineCosts { get; set; }

    public Guid? CreatedByMembershipId { get; set; }
}

/// <summary>
/// food.EventFoodCostSnapshot (§4.8). Append-only (ADR-0037).
/// <para>
/// There is <strong>no <c>IsCurrent</c> flag</strong>: "current" is the newest row
/// per <c>(TenantId, EventId, MealTypeId)</c>. A mutable flag on an append-only
/// table lets two writers disagree about which row is current.
/// </para>
/// </summary>
public class EventFoodCostSnapshot : AppendOnlyTenantScopedEntity
{
    public Guid EventId { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public Guid? MealTypeId { get; set; }

    public int TotalServings { get; set; }

    public decimal TotalCost { get; set; }

    public decimal? CostPerServing { get; set; }

    public decimal? CostPerAttendee { get; set; }

    /// <summary>char(3). The tenant's single currency (TBD-21 / ADR-0046).</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>JSON snapshot — a point-in-time read snapshot.</summary>
    public string? Breakdown { get; set; }
}