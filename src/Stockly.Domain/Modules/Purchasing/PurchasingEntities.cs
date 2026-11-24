using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Master;

namespace Stockly.Domain.Modules.Purchasing;

/// <summary>purchasing.Supplier (§4.6). UQ (filtered) <c>(TenantId, Code) WHERE IsDeleted = 0</c>. <c>Rating</c> does not exist in V1.</summary>
public class Supplier : TenantScopedSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public int? PaymentTermsDays { get; set; }

    /// <summary>char(3). Informational only — V1 is single-currency per tenant (TBD-21), so no FX conversion.</summary>
    public string? CurrencyCode { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

/// <summary>purchasing.SupplierProduct (§4.6). UQ: <c>(TenantId, SupplierId, ProductId)</c>.</summary>
public class SupplierProduct : TenantScopedEntity
{
    public Guid SupplierId { get; set; }

    public Guid ProductId { get; set; }

    public string? SupplierSku { get; set; }

    public Guid UnitId { get; set; }

    public decimal? LastUnitPrice { get; set; }

    public DateTime? LastPurchasedAtUtc { get; set; }

    public decimal? MinimumOrderQuantity { get; set; }

    public bool IsPreferred { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// purchasing.ProductPriceHistory (§4.6). Append-only (no <c>RowVersion</c>, no update, no delete) — ADR-0037.
/// <para>
/// <c>SourceId</c> is a provenance tag only and <strong>deliberately not an FK</strong>:
/// this table is immutable and a dangling reference is harmless (ADR-0038).
/// Current price is the newest row for <c>(TenantId, ProductId, SupplierId)</c>.
/// </para>
/// </summary>
public class ProductPriceHistory : AppendOnlyTenantScopedEntity
{
    public Guid ProductId { get; set; }

    public Guid? SupplierId { get; set; }

    public Guid? WarehouseId { get; set; }

    public Guid UnitId { get; set; }

    /// <summary>CHECK &gt; 0.</summary>
    public decimal UnitPrice { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public PriceSourceType SourceType { get; set; }

    /// <summary>Untyped provenance pointer — not a foreign key (ADR-0038).</summary>
    public Guid? SourceId { get; set; }

    public Guid? CreatedByMembershipId { get; set; }
}

/// <summary>purchasing.PurchaseRequest (§4.6). UQ: <c>(TenantId, RequestNumber)</c>.</summary>
public class PurchaseRequest : TenantScopedEntity
{
    public string RequestNumber { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public PurchaseRequestStatus Status { get; set; }

    public PurchasePriority Priority { get; set; }

    public string? Justification { get; set; }

    public DateOnly? RequiredByDate { get; set; }

    public Guid RequestedByMembershipId { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public Guid? ConvertedPurchaseOrderId { get; set; }

    public string? Notes { get; set; }

    public decimal? TotalEstimatedCost { get; set; }

    public string? IdempotencyKey { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<PurchaseRequestLine> Lines { get; set; } = [];
}

/// <summary>purchasing.PurchaseRequestLine (§4.6). UQ: <c>(PurchaseRequestId, LineNumber)</c>. CHECK: <c>Quantity &gt; 0</c>.</summary>
public class PurchaseRequestLine : TenantScopedEntity
{
    public Guid PurchaseRequestId { get; set; }

    public short LineNumber { get; set; }

    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal? EstimatedUnitCost { get; set; }

    public Guid? PreferredSupplierId { get; set; }

    public string? Notes { get; set; }

    public PurchaseRequest Request { get; set; } = null!;
}

/// <summary>
/// purchasing.PurchaseApproval (§4.6).
/// <para>
/// <c>EntityType</c> is a tag for the API, and the target is expressed with
/// <strong>typed nullable composite FKs</strong> (ADR-0038). A bare untyped
/// <c>EntityId</c> would let an approval exist with no referenced document, which
/// would make it invisible to the approval workflow. A CHECK enforces that exactly
/// one of the two targets is set.
/// </para>
/// </summary>
public class PurchaseApproval : TenantScopedEntity
{
    public ApprovalEntityType EntityType { get; set; }

    public Guid? PurchaseRequestId { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public short StepNumber { get; set; }

    public Guid ApproverMembershipId { get; set; }

    public ApprovalDecision Decision { get; set; }

    public DateTime? DecidedAtUtc { get; set; }

    public string? Comments { get; set; }

    /// <summary>The permission the approver had to hold.</summary>
    public string? RequiredPermissionCode { get; set; }
}

/// <summary>purchasing.PurchaseOrder (§4.6). UQ: <c>(TenantId, OrderNumber)</c>.</summary>
public class PurchaseOrder : TenantScopedEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid? SourceRequestId { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    /// <summary>char(3). The tenant's single currency (TBD-21 / ADR-0046).</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public DateTime? OrderedAtUtc { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ShippingAmount { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>
    /// The documented exception to over-receipt. It exists because
    /// <c>CHECK (ReceivedQuantity &lt;= OrderedQuantity)</c> is deliberately absent
    /// (ADR-0039): a CHECK cannot be relaxed from application code, so the database
    /// would reject the very posting the product allows. The bound is enforced by a
    /// guarded UPDATE instead.
    /// </summary>
    public bool AllowOverReceipt { get; set; }

    public string? Notes { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public Guid CreatedByMembershipId { get; set; }

    public string? IdempotencyKey { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Supplier Supplier { get; set; } = null!;

    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
}

/// <summary>
/// purchasing.PurchaseOrderLine (§4.6). CHECK: <c>OrderedQuantity &gt; 0</c>,
/// <c>ReceivedQuantity &gt;= 0</c>.
/// There is deliberately <strong>no</strong> <c>CHECK (ReceivedQuantity &lt;= OrderedQuantity)</c>
/// — see ADR-0039 and <see cref="PurchaseOrder.AllowOverReceipt"/>.
/// </summary>
public class PurchaseOrderLine : TenantScopedEntity
{
    public Guid PurchaseOrderId { get; set; }

    public short LineNumber { get; set; }

    public Guid ProductId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    public string? Notes { get; set; }

    public PurchaseOrder Order { get; set; } = null!;
}

/// <summary>purchasing.GoodsReceipt (§4.6). UQ: <c>(TenantId, ReceiptNumber)</c>.</summary>
public class GoodsReceipt : TenantScopedEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid? PurchaseOrderId { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid SupplierId { get; set; }

    public string? DeliveryNoteNumber { get; set; }

    public GoodsReceiptStatus Status { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public Guid? PostedStockDocumentId { get; set; }

    public string? Notes { get; set; }

    public Guid? PostedByMembershipId { get; set; }

    public decimal? TotalCost { get; set; }

    public string? IdempotencyKey { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Supplier Supplier { get; set; } = null!;

    public ICollection<GoodsReceiptLine> Lines { get; set; } = [];
}

/// <summary>
/// purchasing.GoodsReceiptLine (§4.6). UQ:
/// <c>(TenantId, GoodsReceiptId, LineNumber)</c>. CHECK: <c>Quantity &gt; 0</c>,
/// <c>UnitCost &gt;= 0</c>.
/// <para>
/// <c>BatchId</c> exists precisely because it is what links the receipt to the lot
/// the stock was posted into; the batch is created by this posting when not
/// supplied. <c>ExpiryDate</c> is required when the product is expiry-tracked.
/// </para>
/// </summary>
public class GoodsReceiptLine : TenantScopedEntity
{
    public Guid GoodsReceiptId { get; set; }

    /// <summary>NULL for a receipt without a purchase order.</summary>
    public Guid? PurchaseOrderLineId { get; set; }

    public short LineNumber { get; set; }

    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitCost { get; set; }

    /// <summary>Required when the product is batch-tracked.</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Required when the product is expiry-tracked.</summary>
    public DateOnly? ExpiryDate { get; set; }

    public DateOnly? ProductionDate { get; set; }

    public string? Notes { get; set; }

    public GoodsReceipt Receipt { get; set; } = null!;
}