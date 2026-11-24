using Stockly.Domain.Common;

namespace Stockly.Domain.Modules.Master;

/// <summary>master.Category (§4.4). UQ (filtered) <c>(TenantId, Code) WHERE IsDeleted = 0</c>; UX <c>(TenantId, ParentCategoryId, Code) WHERE IsDeleted = 0</c> for sibling uniqueness. Depth limit of 3 is a domain rule, tested.</summary>
public class Category : AuditableSoftDeleteTenantScopedEntity
{
    public Guid? ParentCategoryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public Category? Parent { get; set; }

    public ICollection<Category> Children { get; set; } = [];
}

/// <summary>master.Unit (§4.4). UQ (filtered) <c>(TenantId, Code) WHERE IsDeleted = 0</c>.</summary>
public class Unit : TenantScopedSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public UnitType UnitType { get; set; }

    /// <summary>tinyint, 0–4.</summary>
    public byte DecimalPlaces { get; set; }

    public bool IsBaseUnit { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// master.UnitConversion (§4.4). <c>Factor</c> is decimal(18,8) — the single
/// documented precision exception (DATABASE_DESIGN.md §1.5): a conversion factor is
/// a ratio of two quantities and rounding to 4 places would accumulate error
/// across chained conversions.
/// <para>
/// NULL <c>ProductId</c> means a tenant-wide default. Two filtered uniques
/// (ADR-0041) are required because SQL Server treats NULLs as equal only inside
/// an <c>IS NULL</c>-filtered index.
/// </para>
/// </summary>
public class UnitConversion : TenantScopedEntity
{
    /// <summary>NULL = tenant-wide default.</summary>
    public Guid? ProductId { get; set; }

    public Guid FromUnitId { get; set; }

    public Guid ToUnitId { get; set; }

    /// <summary>decimal(18,8), CHECK &gt; 0. CHECK: <c>FromUnitId &lt;&gt; ToUnitId</c>.</summary>
    public decimal Factor { get; set; }
}

/// <summary>
/// master.Product (§4.4). UQ (filtered) <c>(TenantId, Sku) WHERE IsDeleted = 0</c>.
/// CHECK: <c>IsExpiryTracked = 0 OR ShelfLifeDays IS NOT NULL</c>.
/// <para>
/// There is <strong>no <c>Barcode</c> column</strong> (ADR-0033):
/// <see cref="ProductBarcode"/> is the single source of truth for every barcode.
/// <c>IsStockTracked</c> is also deliberately absent: all products are
/// stock-tracked in V1.
/// </para>
/// </summary>
public class Product : TenantScopedSoftDeleteEntity
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid BaseUnitId { get; set; }

    public Guid? PurchaseUnitId { get; set; }

    public bool IsBatchTracked { get; set; }

    public bool IsExpiryTracked { get; set; }

    /// <summary>Required when <see cref="IsExpiryTracked"/>.</summary>
    public int? ShelfLifeDays { get; set; }

    public Guid? DefaultReasonCodeId { get; set; }

    public bool IsActive { get; set; } = true;

    public Category? Category { get; set; }

    public Unit BaseUnit { get; set; } = null!;

    public ICollection<ProductBarcode> Barcodes { get; set; } = [];
}

/// <summary>
/// master.ProductBarcode (§4.4). UQ <c>(TenantId, Barcode)</c> is unfiltered
/// because <c>Barcode</c> is NOT NULL here; UX <c>(ProductId) WHERE IsPrimary = 1</c>
/// makes "primary" a single row rather than a second column on Product (ADR-0033).
/// </summary>
public class ProductBarcode : TenantScopedEntity
{
    public Guid ProductId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    /// <summary>An unknown symbology is stored as <see cref="Common.BarcodeSymbology.Code128"/> rather than rejected (TBD-18 / ADR-0043).</summary>
    public BarcodeSymbology Symbology { get; set; }

    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
}

/// <summary>
/// master.ProductWarehouseSetting (§4.4). UQ:
/// <c>(TenantId, WarehouseId, ProductId)</c>. CHECK:
/// <c>MaximumQuantity IS NULL OR MinimumQuantity IS NULL OR MaximumQuantity &gt;= MinimumQuantity</c>.
/// </summary>
public class ProductWarehouseSetting : TenantScopedEntity
{
    public Guid WarehouseId { get; set; }

    public Guid ProductId { get; set; }

    public decimal? MinimumQuantity { get; set; }

    public decimal? MaximumQuantity { get; set; }

    public decimal? ReorderQuantity { get; set; }

    public Guid? DefaultSupplierId { get; set; }

    public int? ShelfLifeDaysOverride { get; set; }

    public bool IsPreferred { get; set; }
}

/// <summary>master.ReasonCode (§4.4). UQ (filtered) <c>(TenantId, MovementType, Code) WHERE IsDeleted = 0</c>.</summary>
public class ReasonCode : TenantScopedSoftDeleteEntity
{
    public MovementType MovementType { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public bool IsActive { get; set; } = true;
}