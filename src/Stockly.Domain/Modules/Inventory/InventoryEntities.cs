using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Master;

namespace Stockly.Domain.Modules.Inventory;

/// <summary>
/// inventory.Batch (§4.5) — batch identity, not a stock row.
/// <para>
/// Two uniques are required (ADR-0041): <c>(TenantId, ProductId, BatchNumber, ExpiryDate)</c>
/// and <c>(TenantId, ProductId, BatchNumber) WHERE ExpiryDate IS NULL</c>, because
/// SQL Server treats NULLs as equal only inside an <c>IS NULL</c>-filtered index.
/// CHECK: <c>ExpiryDate IS NULL OR (ProductionDate IS NULL OR ExpiryDate &gt;= ProductionDate)</c>.
/// </para>
/// </summary>
public class Batch : TenantScopedEntity
{
    public Guid ProductId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>NULL only when the product is not expiry-tracked.</summary>
    public DateOnly? ExpiryDate { get; set; }

    public DateOnly? ProductionDate { get; set; }

    /// <summary>Source supplier.</summary>
    public Guid? SupplierId { get; set; }

    public decimal? DefaultUnitCost { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public bool IsQuarantined { get; set; }

    public Product Product { get; set; } = null!;
}

/// <summary>
/// inventory.InventoryLot (§4.5) — batch × warehouse. Concurrency-controlled.
/// <para>
/// <c>ExpirySortKey</c> is persisted so FEFO is a single index seek with no join
/// to <see cref="Batch"/> (ADR-0041). The two filtered uniques
/// (<c>WHERE BatchId IS NULL</c> and <c>WHERE BatchId IS NOT NULL</c>) are
/// mutually exclusive and both required: dropping the first would leave the
/// guarded UPDATE of §8.1 with no single row to hit.
/// </para>
/// CHECK: <c>OnHandQuantity &gt;= 0</c>, <c>ReservedQuantity &gt;= 0</c>,
/// <c>ReservedQuantity &lt;= OnHandQuantity</c>.
/// <para>
/// <c>ReservedQuantity</c> is always 0 in V1 — reservations are undecided
/// (TBD-26 / ADR-0050) and must not be read as a soft-reservation design.
/// </para>
/// </summary>
public class InventoryLot : TenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>NULL = non-batched product.</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Copied from <see cref="Batch.ExpiryDate"/>, or <c>9999-12-31</c> for non-expiry-tracked lots.</summary>
    public DateOnly ExpirySortKey { get; set; }

    public decimal OnHandQuantity { get; set; }

    /// <summary>Always 0 in V1 (TBD-26 / ADR-0050).</summary>
    public decimal ReservedQuantity { get; set; }

    public DateTime FirstReceivedAtUtc { get; set; }

    public DateTime? LastReceivedAtUtc { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public Batch? Batch { get; set; }
}

/// <summary>
/// inventory.StockBalance (§4.5) — product × warehouse. Concurrency-controlled.
/// UX: <c>(TenantId, WarehouseId, ProductId)</c>, <c>TenantId</c> leading (ADR-0032).
/// <para>
/// The rebuild is <strong>partial</strong> and must be documented as such: it
/// recomputes <c>OnHandQuantity</c>, <c>AverageUnitCost</c> and
/// <c>LastMovementAtUtc</c> from <see cref="StockTransaction"/>, restores
/// <c>IncomingQuantity</c> from open PO lines and <c>LastCountedAtUtc</c> from
/// posted counts, and leaves <c>ReservedQuantity</c> at 0.
/// </para>
/// </summary>
public class StockBalance : TenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    public Guid ProductId { get; set; }

    public decimal OnHandQuantity { get; set; }

    /// <summary>Always 0 in V1 (TBD-26 / ADR-0050).</summary>
    public decimal ReservedQuantity { get; set; }

    /// <summary>On open purchase orders. <strong>Not</strong> ledger-rebuildable.</summary>
    public decimal IncomingQuantity { get; set; }

    /// <summary>Moving weighted average (ADR-0017).</summary>
    public decimal AverageUnitCost { get; set; }

    public DateTime? LastMovementAtUtc { get; set; }

    /// <summary><strong>Not</strong> ledger-rebuildable.</summary>
    public DateTime? LastCountedAtUtc { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

/// <summary>
/// inventory.StockDocument (§4.5) — movement header.
/// <para>
/// <strong>There is no polymorphic <c>(SourceType, SourceDocumentId)</c> pair</strong>
/// (ADR-0038). SQL Server cannot express a filtered FK to a table chosen at
/// runtime, so a bare <c>uniqueidentifier</c> would have let the database accept a
/// document pointing at nothing. Typed nullable composite FKs make the reference
/// real, and a CHECK enforces that exactly one of them is set when
/// <c>SourceType &lt;&gt; 0</c>.
/// </para>
/// <para>
/// <c>CounterWarehouseId</c> is the entire transfer model (TBD-22 / ADR-0047): a
/// transfer is one document with <c>MovementType = 4</c> whose two
/// <see cref="StockTransaction"/> legs are written in one database transaction.
/// There is no in-transit entity, no in-transit warehouse and no transfer state
/// machine. The composite FK on <c>CounterWarehouseId</c> makes a cross-tenant
/// transfer unrepresentable.
/// </para>
/// </summary>
public class StockDocument : TenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    /// <summary>Target warehouse for TRANSFER. The pairing invariant is <c>CounterWarehouseId IS NOT NULL ⇔ MovementType = 4</c>.</summary>
    public Guid? CounterWarehouseId { get; set; }

    /// <summary>Allocated from ops.DocumentSequence — never <c>MAX(number)+1</c> in application code.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    public MovementType MovementType { get; set; }

    public StockDocumentStatus Status { get; set; }

    public Guid? SourceGoodsReceiptId { get; set; }

    public Guid? SourceStockCountId { get; set; }

    public Guid? SourceEventConsumptionId { get; set; }

    /// <summary>A tag for the API, not the referential link.</summary>
    public StockDocumentSourceType SourceType { get; set; }

    public Guid? EventId { get; set; }

    /// <summary>Required for Adjustment/Waste.</summary>
    public Guid? ReasonCodeId { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    /// <summary>Denormalised header total, written in the same transaction as the lines (DATABASE_DESIGN §1.4).</summary>
    public decimal TotalQuantity { get; set; }

    public decimal TotalCost { get; set; }

    /// <summary>Business time.</summary>
    public DateTime OccurredAtUtc { get; set; }

    public DateTime? PostedAtUtc { get; set; }

    public Guid? PostedByMembershipId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? SessionId { get; set; }

    public string? CorrelationId { get; set; }

    public Guid? ReversedByDocumentId { get; set; }

    public Guid? ReversesDocumentId { get; set; }

    /// <summary>UX (filtered) <c>(TenantId, IdempotencyKey) WHERE IdempotencyKey IS NOT NULL</c>.</summary>
    public string? IdempotencyKey { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<StockDocumentLine> Lines { get; set; } = [];
}

/// <summary>
/// inventory.StockDocumentLine (§4.5). UX:
/// <c>(TenantId, StockDocumentId, LineNumber)</c>. CHECK: <c>Quantity &gt; 0</c>.
/// <para>
/// There is deliberately <strong>no <c>BalanceAfter</c> column</strong>
/// (ADR-0037): a line may split across several lots, so "the balance after this
/// line" has no single value. The authoritative running balance lives on
/// <see cref="StockTransaction.BalanceAfter"/>.
/// </para>
/// </summary>
public class StockDocumentLine : TenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    public Guid StockDocumentId { get; set; }

    public short LineNumber { get; set; }

    public Guid ProductId { get; set; }

    public Guid? BatchId { get; set; }

    /// <summary>CHECK &gt; 0. Never negative: direction is carried by the ledger, not by a signed column.</summary>
    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Notes { get; set; }

    public StockDocument Document { get; set; } = null!;
}

/// <summary>
/// inventory.StockTransaction (§4.5) — the immutable quantity and valuation ledger.
/// <para>
/// <strong>No <c>RowVersion</c>, no <c>IsDeleted</c>, no update path.</strong> The
/// application principal holds <c>INSERT</c> and <c>SELECT</c> only. A movement is
/// corrected by a reversing entry, never by editing history (ADR-0037).
/// <c>Direction</c> is a column rather than a sign because a mis-signed value
/// cannot then be misread as a receipt.
/// </para>
/// </summary>
public class StockTransaction : AppendOnlyTenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    public Guid StockDocumentId { get; set; }

    public Guid StockDocumentLineId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? BatchId { get; set; }

    public MovementType MovementType { get; set; }

    public MovementDirection Direction { get; set; }

    /// <summary>CHECK &gt; 0. Always positive; sign is carried by <see cref="Direction"/>.</summary>
    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? UnitCost { get; set; }

    /// <summary>Running balance, mandatory — one row per lot allocation.</summary>
    public decimal BalanceAfter { get; set; }

    public Guid? EventId { get; set; }

    public Guid? ReasonCodeId { get; set; }

    public string? Reference { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime PostedAtUtc { get; set; }

    public Guid UserId { get; set; }

    public Guid MembershipId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? SessionId { get; set; }

    public string? CorrelationId { get; set; }

    public bool IsReversal { get; set; }
}

/// <summary>inventory.StockCount (§4.5). UX: <c>(TenantId, WarehouseId, CountNumber)</c>.</summary>
public class StockCount : AuditableTenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    public string CountNumber { get; set; } = string.Empty;

    public StockCountStatus Status { get; set; }

    public StockCountScopeType ScopeType { get; set; }

    public Guid? CategoryId { get; set; }

    public DateTime CutoffAtUtc { get; set; }

    public DateTime? CountedAtUtc { get; set; }

    public Guid? PostedDocumentId { get; set; }

    public Guid? PostedByMembershipId { get; set; }

    public string? Notes { get; set; }

    public string? IdempotencyKey { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<StockCountLine> Lines { get; set; } = [];
}

/// <summary>
/// inventory.StockCountLine (§4.5). Requires two uniques (ADR-0041):
/// <c>(TenantId, StockCountId, ProductId, BatchId)</c> and
/// <c>(TenantId, StockCountId, ProductId) WHERE BatchId IS NULL</c> — without the
/// filtered variant two count lines for the same non-batched product would both be
/// allowed. CHECK: <c>VarianceQuantity IS NULL OR VarianceQuantity = CountedQuantity - ExpectedQuantity</c>.
/// </summary>
public class StockCountLine : TenantScopedEntity
{
    public Guid StockCountId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? BatchId { get; set; }

    public Guid UnitId { get; set; }

    public decimal ExpectedQuantity { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal? VarianceQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public Guid? CountedByMembershipId { get; set; }

    public DateTime? CountedAtUtc { get; set; }

    public StockCountResolution Resolution { get; set; }

    public string? Notes { get; set; }

    public StockCount Count { get; set; } = null!;
}