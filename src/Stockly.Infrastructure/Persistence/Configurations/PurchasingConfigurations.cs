using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Inventory;
using Stockly.Domain.Modules.Master;
using Stockly.Domain.Modules.Purchasing;

namespace Stockly.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("Supplier");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.TaxNumber).HasMaxLength(50);
        b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsFixedLength();
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // No Rating column in V1: the previous draft shipped a field that nothing
        // computed or read, since supplier scoring is a V2 feature.
        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Supplier_TenantId_Code");

        b.HasIndex(x => new { x.TenantId, x.Name }).HasDatabaseName("IX_Supplier_TenantId_Name");
    }
}

internal sealed class SupplierProductConfiguration : IEntityTypeConfiguration<SupplierProduct>
{
    public void Configure(EntityTypeBuilder<SupplierProduct> b)
    {
        b.ToTable("SupplierProduct");
        b.HasKey(x => x.Id);
        b.Property(x => x.SupplierSku).HasMaxLength(50);
        b.Property(x => x.LastUnitPrice).Money_();
        b.Property(x => x.MinimumOrderQuantity).Money_();
        b.Property(x => x.IsPreferred).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(x => new { x.SupplierId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_SupplierProduct_Supplier");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_SupplierProduct_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_SupplierProduct_Unit");

        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_SupplierProduct_TenantId_SupplierId_ProductId");

        b.HasIndex(x => new { x.TenantId, x.ProductId }).HasDatabaseName("IX_SupplierProduct_TenantId_ProductId");
    }
}

internal sealed class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> b)
    {
        b.ToTable("ProductPriceHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.UnitPrice).Money_().IsRequired();

        // SourceId is a bare untyped uniqueidentifier with NO foreign key on purpose
        // (ADR-0038): the table is append-only and a dangling provenance pointer is
        // harmless. SourceType carries the meaning for the API.

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductPriceHistory_Product");

        b.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(x => new { x.SupplierId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductPriceHistory_Supplier");

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductPriceHistory_Warehouse");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductPriceHistory_Unit");

        b.HasIndex(x => new { x.TenantId, x.ProductId, x.EffectiveFromUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_ProductPriceHistory_TenantId_ProductId_EffectiveFromUtc");

        b.ToTable("ProductPriceHistory", t =>
        {
            t.HasCheckConstraint("CK_ProductPriceHistory_UnitPrice", "[UnitPrice] > 0");
            t.HasCheckConstraint("CK_ProductPriceHistory_SourceType", "[SourceType] IN (1,2,3)");
        });
    }
}

internal sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> b)
    {
        b.ToTable("PurchaseRequest");
        b.HasKey(x => x.Id);
        b.Property(x => x.RequestNumber).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.Priority).Enum_().IsRequired();
        b.Property(x => x.Justification).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.RequiredByDate).HasColumnType("date");
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.TotalEstimatedCost).Money_();
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseRequest_Warehouse");

        b.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.ConvertedPurchaseOrderId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseRequest_PurchaseOrder");

        b.HasIndex(x => new { x.TenantId, x.RequestNumber }).IsUnique().HasDatabaseName("UX_PurchaseRequest_TenantId_RequestNumber");
        b.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("IX_PurchaseRequest_TenantId_Status");

        b.ToTable("PurchaseRequest", t =>
        {
            t.HasCheckConstraint("CK_PurchaseRequest_Status", "[Status] IN (1,2,3,4,5,6)");
            t.HasCheckConstraint("CK_PurchaseRequest_Priority", "[Priority] IN (1,2,3)");
        });
    }
}

internal sealed class PurchaseRequestLineConfiguration : IEntityTypeConfiguration<PurchaseRequestLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestLine> b)
    {
        b.ToTable("PurchaseRequestLine");
        b.HasKey(x => x.Id);
        b.Property(x => x.LineNumber).IsRequired();
        b.Property(x => x.Quantity).Money_().IsRequired();
        b.Property(x => x.EstimatedUnitCost).Money_();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Request)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => new { x.PurchaseRequestId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseRequestLine_PurchaseRequest");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseRequestLine_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseRequestLine_Unit");

        b.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(x => new { x.PreferredSupplierId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseRequestLine_PreferredSupplier");

        b.HasIndex(x => new { x.PurchaseRequestId, x.LineNumber }).IsUnique().HasDatabaseName("UX_PurchaseRequestLine_PurchaseRequestId_LineNumber");

        b.ToTable("PurchaseRequestLine", t =>
            t.HasCheckConstraint("CK_PurchaseRequestLine_Quantity", "[Quantity] > 0"));
    }
}

internal sealed class PurchaseApprovalConfiguration : IEntityTypeConfiguration<PurchaseApproval>
{
    public void Configure(EntityTypeBuilder<PurchaseApproval> b)
    {
        b.ToTable("PurchaseApproval");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).Enum_().IsRequired();
        b.Property(x => x.StepNumber).IsRequired();
        b.Property(x => x.Decision).Enum_().IsRequired();
        b.Property(x => x.Comments).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.RequiredPermissionCode).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<PurchaseRequest>()
            .WithMany()
            .HasForeignKey(x => new { x.PurchaseRequestId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseApproval_PurchaseRequest");

        b.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.PurchaseOrderId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseApproval_PurchaseOrder");

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.ApproverMembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseApproval_TenantMembership");

        b.HasIndex(x => new { x.TenantId, x.EntityType, x.StepNumber, x.ApproverMembershipId })
            .IsUnique()
            .HasDatabaseName("UX_PurchaseApproval_TenantId_EntityType_StepNumber_ApproverMembershipId");

        b.HasIndex(x => new { x.TenantId, x.EntityType, x.ApproverMembershipId })
            .IsUnique()
            .HasFilter("[Decision] = 1")
            .HasDatabaseName("UX_PurchaseApproval_PendingPerApprover");

        b.ToTable("PurchaseApproval", t =>
        {
            t.HasCheckConstraint("CK_PurchaseApproval_EntityType", "[EntityType] IN (1,2)");
            t.HasCheckConstraint("CK_PurchaseApproval_Decision", "[Decision] IN (1,2,3)");

            // Exactly one target. An approval pointing at no document would be
            // invisible to the approval workflow (ADR-0038).
            t.HasCheckConstraint(
                "CK_PurchaseApproval_SingleTarget",
                "([EntityType] = 1 AND [PurchaseRequestId] IS NOT NULL AND [PurchaseOrderId] IS NULL) OR " +
                "([EntityType] = 2 AND [PurchaseRequestId] IS NULL AND [PurchaseOrderId] IS NOT NULL)");
        });
    }
}

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("PurchaseOrder");
        b.HasKey(x => x.Id);
        b.Property(x => x.OrderNumber).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsFixedLength().IsRequired();
        b.Property(x => x.ExpectedDeliveryDate).HasColumnType("date");
        b.Property(x => x.Subtotal).Money_().IsRequired();
        b.Property(x => x.TaxAmount).Money_().IsRequired();
        b.Property(x => x.ShippingAmount).Money_().IsRequired();
        b.Property(x => x.TotalAmount).Money_().IsRequired();
        b.Property(x => x.AllowOverReceipt).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => new { x.SupplierId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseOrder_Supplier");

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseOrder_Warehouse");

        b.HasOne<PurchaseRequest>()
            .WithMany()
            .HasForeignKey(x => new { x.SourceRequestId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseOrder_PurchaseRequest");

        b.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique().HasDatabaseName("UX_PurchaseOrder_TenantId_OrderNumber");
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.Status }).HasDatabaseName("IX_PurchaseOrder_TenantId_SupplierId_Status");
        b.HasIndex(x => new { x.TenantId, x.Status, x.ExpectedDeliveryDate }).HasDatabaseName("IX_PurchaseOrder_TenantId_Status_ExpectedDeliveryDate");

        b.ToTable("PurchaseOrder", t =>
            t.HasCheckConstraint("CK_PurchaseOrder_Status", "[Status] IN (1,2,3,4,5,6,7)"));
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("PurchaseOrderLine");
        b.HasKey(x => x.Id);
        b.Property(x => x.LineNumber).IsRequired();
        b.Property(x => x.OrderedQuantity).Money_().IsRequired();
        b.Property(x => x.ReceivedQuantity).Money_().IsRequired();
        b.Property(x => x.UnitPrice).Money_().IsRequired();
        b.Property(x => x.LineTotal).Money_().IsRequired();
        b.Property(x => x.ExpectedDate).HasColumnType("date");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Order)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => new { x.PurchaseOrderId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseOrderLine_PurchaseOrder");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseOrderLine_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_PurchaseOrderLine_Unit");

        b.HasIndex(x => new { x.TenantId, x.PurchaseOrderId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("UX_PurchaseOrderLine_TenantId_PurchaseOrderId_LineNumber");

        b.HasIndex(x => new { x.TenantId, x.ProductId }).HasDatabaseName("IX_PurchaseOrderLine_TenantId_ProductId");

        b.ToTable("PurchaseOrderLine", t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrderLine_OrderedQuantity", "[OrderedQuantity] > 0");
            t.HasCheckConstraint("CK_PurchaseOrderLine_ReceivedQuantity", "[ReceivedQuantity] >= 0");

            // DELIBERATELY ABSENT (ADR-0039):
            //   CHECK (ReceivedQuantity <= OrderedQuantity)
            // Over-receipt is a legal business state when the parent allows it, and a
            // CHECK cannot be relaxed from application code — the database would reject
            // the very posting the product allows. The bound is enforced by a guarded
            // UPDATE plus the purchase_order_over_receipt error and an audit record.
            // CHECK constraints are reserved for absolute invariants with no legitimate
            // exception.
        });
    }
}

internal sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> b)
    {
        b.ToTable("GoodsReceipt");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReceiptNumber).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.DeliveryNoteNumber).HasMaxLength(100);
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.TotalCost).Money_();
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceipt_Warehouse");

        b.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => new { x.SupplierId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceipt_Supplier");

        b.HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.PurchaseOrderId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceipt_PurchaseOrder");

        b.HasOne<StockDocument>()
            .WithMany()
            .HasForeignKey(x => new { x.PostedStockDocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceipt_StockDocument");

        b.HasIndex(x => new { x.TenantId, x.ReceiptNumber }).IsUnique().HasDatabaseName("UX_GoodsReceipt_TenantId_ReceiptNumber");
        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ReceivedAtUtc }).HasDatabaseName("IX_GoodsReceipt_TenantId_WarehouseId_ReceivedAtUtc");

        b.ToTable("GoodsReceipt", t =>
            t.HasCheckConstraint("CK_GoodsReceipt_Status", "[Status] IN (1,2,3)"));
    }
}

internal sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> b)
    {
        b.ToTable("GoodsReceiptLine");
        b.HasKey(x => x.Id);
        b.Property(x => x.LineNumber).IsRequired();
        b.Property(x => x.Quantity).Money_().IsRequired();
        b.Property(x => x.UnitCost).Money_().IsRequired();
        b.Property(x => x.ExpiryDate).HasColumnType("date");
        b.Property(x => x.ProductionDate).HasColumnType("date");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Receipt)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => new { x.GoodsReceiptId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceiptLine_GoodsReceipt");

        b.HasOne<PurchaseOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.PurchaseOrderLineId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceiptLine_PurchaseOrderLine");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceiptLine_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceiptLine_Unit");

        // This BatchId is what links the receipt to the lot the stock was posted into.
        // A previous draft referenced "the batch created here" on a table that had no
        // such column.
        b.HasOne<Batch>()
            .WithMany()
            .HasForeignKey(x => new { x.BatchId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_GoodsReceiptLine_Batch");

        b.HasIndex(x => new { x.TenantId, x.GoodsReceiptId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("UX_GoodsReceiptLine_TenantId_GoodsReceiptId_LineNumber");

        b.ToTable("GoodsReceiptLine", t =>
        {
            t.HasCheckConstraint("CK_GoodsReceiptLine_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_GoodsReceiptLine_UnitCost", "[UnitCost] >= 0");
        });
    }
}