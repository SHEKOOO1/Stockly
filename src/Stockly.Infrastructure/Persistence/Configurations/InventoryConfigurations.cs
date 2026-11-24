using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Events;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Inventory;
using Stockly.Domain.Modules.Purchasing;
using Stockly.Domain.Modules.Master;

namespace Stockly.Infrastructure.Persistence.Configurations;

internal sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> b)
    {
        b.ToTable("Batch");
        b.HasKey(x => x.Id);
        b.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.ExpiryDate).HasColumnType("date");
        b.Property(x => x.ProductionDate).HasColumnType("date");
        b.Property(x => x.DefaultUnitCost).Money_();
        b.Property(x => x.IsQuarantined).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Batch_Product");

        b.HasOne<Supplier>()
                    .WithMany()
                    .HasForeignKey(x => new { x.SupplierId, x.TenantId })
                    .HasPrincipalKey(x => new { x.Id, x.TenantId })
                    .HasConstraintName("FK_Batch_Supplier");

        // Both uniques are required (ADR-0041): SQL Server treats NULLs as equal only
        // inside an IS NULL-filtered index, so the non-expiry variant needs its own.
        b.HasIndex(x => new { x.TenantId, x.ProductId, x.BatchNumber, x.ExpiryDate })
            .IsUnique()
            .HasDatabaseName("UX_Batch_TenantId_ProductId_BatchNumber_ExpiryDate");

        b.HasIndex(x => new { x.TenantId, x.ProductId, x.BatchNumber })
            .IsUnique()
            .HasFilter("[ExpiryDate] IS NULL")
            .HasDatabaseName("UX_Batch_TenantId_ProductId_BatchNumber_NoExpiry");

        b.ToTable("Batch", t =>
            t.HasCheckConstraint(
                "CK_Batch_Dates",
                "[ExpiryDate] IS NULL OR ([ProductionDate] IS NULL OR [ExpiryDate] >= [ProductionDate])"));
    }
}

internal sealed class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> b)
    {
        b.ToTable("InventoryLot");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExpirySortKey).HasColumnType("date").IsRequired();
        b.Property(x => x.OnHandQuantity).Money_().IsRequired();
        b.Property(x => x.ReservedQuantity).Money_().IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_InventoryLot_Warehouse");

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_InventoryLot_Product");

        b.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => new { x.BatchId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_InventoryLot_Batch");

        // The two predicates are MUTUALLY EXCLUSIVE, so together they admit exactly one
        // lot per (tenant, warehouse, product) for untracked products and exactly one
        // per (tenant, warehouse, batch) for tracked products. Both are required:
        // dropping the first would leave the guarded UPDATE of DATABASE_DESIGN §8.1
        // with no single row to hit.
        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId })
            .IsUnique()
            .HasFilter("[BatchId] IS NULL")
            .HasDatabaseName("UX_InventoryLot_NonBatched");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.BatchId })
            .IsUnique()
            .HasFilter("[BatchId] IS NOT NULL")
            .HasDatabaseName("UX_InventoryLot_Batch");

        b.HasIndex(x => new { x.TenantId, x.ProductId }).HasDatabaseName("IX_InventoryLot_TenantId_ProductId");
        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.OnHandQuantity }).HasDatabaseName("IX_InventoryLot_TenantId_WarehouseId_OnHandQuantity");

        // FEFO allocation is an index seek, not a sort (ADR-0041).
        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId, x.ExpirySortKey, x.FirstReceivedAtUtc, x.Id })
            .IncludeProperties(x => x.OnHandQuantity)
            .HasDatabaseName("IX_InventoryLot_FEFO");

        b.ToTable("InventoryLot", t =>
        {
            t.HasCheckConstraint("CK_InventoryLot_OnHandQuantity", "[OnHandQuantity] >= 0");
            t.HasCheckConstraint("CK_InventoryLot_ReservedQuantity", "[ReservedQuantity] >= 0");
            t.HasCheckConstraint("CK_InventoryLot_ReservedWithinOnHand", "[ReservedQuantity] <= [OnHandQuantity]");
        });
    }
}

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> b)
    {
        b.ToTable("StockBalance");
        b.HasKey(x => x.Id);
        b.Property(x => x.OnHandQuantity).Money_().IsRequired();
        b.Property(x => x.ReservedQuantity).Money_().IsRequired();
        b.Property(x => x.IncomingQuantity).Money_().IsRequired();
        b.Property(x => x.AverageUnitCost).Money_().IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockBalance_Warehouse");

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockBalance_Product");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_StockBalance");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.OnHandQuantity }).HasDatabaseName("IX_StockBalance_TenantId_WarehouseId_OnHandQuantity");
        b.HasIndex(x => new { x.TenantId, x.ProductId }).HasDatabaseName("IX_StockBalance_TenantId_ProductId");

        b.ToTable("StockBalance", t =>
        {
            t.HasCheckConstraint("CK_StockBalance_OnHandQuantity", "[OnHandQuantity] >= 0");
            t.HasCheckConstraint("CK_StockBalance_ReservedQuantity", "[ReservedQuantity] >= 0");
            t.HasCheckConstraint("CK_StockBalance_ReservedWithinOnHand", "[ReservedQuantity] <= [OnHandQuantity]");
        });
    }
}

internal sealed class StockDocumentConfiguration : IEntityTypeConfiguration<StockDocument>
{
    public void Configure(EntityTypeBuilder<StockDocument> b)
    {
        b.ToTable("StockDocument");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNumber).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.MovementType).Enum_().IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.SourceType).Enum_().IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.TotalQuantity).Money_().IsRequired();
        b.Property(x => x.TotalCost).Money_().IsRequired();
        b.Property(x => x.CorrelationId).HasMaxLength(100);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_Warehouse");

        // CounterWarehouseId is the entire transfer model (TBD-22 / ADR-0047). The
        // composite FK makes a CROSS-TENANT transfer unrepresentable at the database
        // level — the strongest form of that guarantee available.
        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.CounterWarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_CounterWarehouse");

        // Typed nullable composite FKs instead of a polymorphic (SourceType, SourceId)
        // pair (ADR-0038): SQL Server cannot express a filtered FK to a table chosen at
        // runtime, so a bare uniqueidentifier would let the database accept a document
        // pointing at nothing.
        b.HasOne<GoodsReceipt>()
            .WithMany()
            .HasForeignKey(x => new { x.SourceGoodsReceiptId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_GoodsReceipt");

        b.HasOne<StockCount>()
            .WithMany()
            .HasForeignKey(x => new { x.SourceStockCountId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_StockCount");

        b.HasOne<EventConsumption>()
            .WithMany()
            .HasForeignKey(x => new { x.SourceEventConsumptionId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_EventConsumption");

        b.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_Event");

        b.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.ReasonCodeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_ReasonCode");

        b.HasOne<StockDocument>()
            .WithMany()
            .HasForeignKey(x => new { x.ReversesDocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_Reverses");

        b.HasOne<StockDocument>()
            .WithMany()
            .HasForeignKey(x => new { x.ReversedByDocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocument_ReversedBy");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.DocumentNumber })
            .IsUnique()
            .HasDatabaseName("UX_StockDocument_TenantId_WarehouseId_DocumentNumber");

        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_StockDocument_IdempotencyKey");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.PostedAtUtc }).HasDatabaseName("IX_StockDocument_TenantId_WarehouseId_PostedAtUtc");
        b.HasIndex(x => new { x.TenantId, x.MovementType, x.PostedAtUtc }).HasDatabaseName("IX_StockDocument_TenantId_MovementType_PostedAtUtc");
        b.HasIndex(x => new { x.TenantId, x.EventId }).HasDatabaseName("IX_StockDocument_TenantId_EventId");
        b.HasIndex(x => new { x.TenantId, x.SourceGoodsReceiptId }).HasDatabaseName("IX_StockDocument_TenantId_SourceGoodsReceiptId");
        b.HasIndex(x => new { x.TenantId, x.SourceStockCountId }).HasDatabaseName("IX_StockDocument_TenantId_SourceStockCountId");
        b.HasIndex(x => new { x.TenantId, x.SourceEventConsumptionId }).HasDatabaseName("IX_StockDocument_TenantId_SourceEventConsumptionId");

        b.ToTable("StockDocument", t =>
        {
            t.HasCheckConstraint("CK_StockDocument_MovementType", "[MovementType] IN (1,2,3,4,5,6,7)");
            t.HasCheckConstraint("CK_StockDocument_Status", "[Status] IN (1,2,3,4)");
            t.HasCheckConstraint("CK_StockDocument_SourceType", "[SourceType] IN (0,1,2,3)");
            t.HasCheckConstraint("CK_StockDocument_PostedStatus", "[PostedAtUtc] IS NULL OR [Status] IN (2,4)");

            // The pairing invariant of the transfer model: a counter-warehouse exists
            // if and only if this is a transfer.
            t.HasCheckConstraint(
                "CK_StockDocument_TransferPairing",
                "([MovementType] = 4 AND [CounterWarehouseId] IS NOT NULL) OR ([MovementType] <> 4 AND [CounterWarehouseId] IS NULL)");

            // Exactly one typed source when SourceType <> 0. An absolute invariant, so
            // a CHECK is the correct tool.
            t.HasCheckConstraint(
                "CK_StockDocument_SingleSource",
                "([SourceType] = 0 AND [SourceGoodsReceiptId] IS NULL AND [SourceStockCountId] IS NULL AND [SourceEventConsumptionId] IS NULL) OR " +
                "([SourceType] = 1 AND [SourceGoodsReceiptId] IS NOT NULL AND [SourceStockCountId] IS NULL AND [SourceEventConsumptionId] IS NULL) OR " +
                "([SourceType] = 2 AND [SourceGoodsReceiptId] IS NULL AND [SourceStockCountId] IS NOT NULL AND [SourceEventConsumptionId] IS NULL) OR " +
                "([SourceType] = 3 AND [SourceGoodsReceiptId] IS NULL AND [SourceStockCountId] IS NULL AND [SourceEventConsumptionId] IS NOT NULL)");
        });
    }
}

internal sealed class StockDocumentLineConfiguration : IEntityTypeConfiguration<StockDocumentLine>
{
    public void Configure(EntityTypeBuilder<StockDocumentLine> b)
    {
        b.ToTable("StockDocumentLine");
        b.HasKey(x => x.Id);
        b.Property(x => x.LineNumber).IsRequired();
        b.Property(x => x.Quantity).Money_().IsRequired();
        b.Property(x => x.UnitCost).Money_();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Document)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => new { x.StockDocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocumentLine_StockDocument");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocumentLine_Product");

        b.HasOne<Batch>()
            .WithMany()
            .HasForeignKey(x => new { x.BatchId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocumentLine_Batch");

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocumentLine_Warehouse");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockDocumentLine_Unit");

        b.HasIndex(x => new { x.TenantId, x.StockDocumentId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("UX_StockDocumentLine_TenantId_StockDocumentId_LineNumber");

        b.HasIndex(x => new { x.TenantId, x.ProductId }).HasDatabaseName("IX_StockDocumentLine_TenantId_ProductId");
        b.HasIndex(x => new { x.TenantId, x.BatchId }).HasDatabaseName("IX_StockDocumentLine_TenantId_BatchId");

        b.ToTable("StockDocumentLine", t =>
            t.HasCheckConstraint("CK_StockDocumentLine_Quantity", "[Quantity] > 0"));
    }
}

internal sealed class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> b)
    {
        b.ToTable("StockTransaction");
        b.HasKey(x => x.Id);
        b.Property(x => x.MovementType).Enum_().IsRequired();
        b.Property(x => x.Direction).Enum_().IsRequired();
        b.Property(x => x.Quantity).Money_().IsRequired();
        b.Property(x => x.UnitCost).Money_();
        b.Property(x => x.BalanceAfter).Money_().IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.CorrelationId).HasMaxLength(100);
        b.Property(x => x.IsReversal).IsRequired();

        // Deliberately NOT IsRowVersion() and no IsDeleted: the table is append-only and
        // the database principal holds INSERT + SELECT only (ADR-0037). EF must not
        // synthesise a concurrency token for an immutable row.

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_Warehouse");

        b.HasOne<StockDocument>()
            .WithMany()
            .HasForeignKey(x => new { x.StockDocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_StockDocument");

        b.HasOne<StockDocumentLine>()
            .WithMany()
            .HasForeignKey(x => new { x.StockDocumentLineId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_StockDocumentLine");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_Product");

        b.HasOne<Batch>()
            .WithMany()
            .HasForeignKey(x => new { x.BatchId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_Batch");

        b.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_Event");

        b.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.ReasonCodeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockTransaction_ReasonCode");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.PostedAtUtc }).HasDatabaseName("IX_StockTransaction_TenantId_WarehouseId_PostedAtUtc");
        b.HasIndex(x => new { x.TenantId, x.ProductId, x.PostedAtUtc }).HasDatabaseName("IX_StockTransaction_TenantId_ProductId_PostedAtUtc");
        b.HasIndex(x => new { x.TenantId, x.EventId }).HasDatabaseName("IX_StockTransaction_TenantId_EventId");
        b.HasIndex(x => new { x.TenantId, x.MovementType, x.PostedAtUtc }).HasDatabaseName("IX_StockTransaction_TenantId_MovementType_PostedAtUtc");

        b.ToTable("StockTransaction", t =>
        {
            t.HasCheckConstraint("CK_StockTransaction_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_StockTransaction_Direction", "[Direction] IN (1,2)");
            t.HasCheckConstraint("CK_StockTransaction_BalanceAfter", "[BalanceAfter] >= 0");
        });
    }
}

internal sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> b)
    {
        b.ToTable("StockCount");
        b.HasKey(x => x.Id);
        b.Property(x => x.CountNumber).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.ScopeType).Enum_().IsRequired();
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockCount_Warehouse");

        b.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => new { x.CategoryId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockCount_Category");

        b.HasOne<StockDocument>()
                    .WithMany()
                    .HasForeignKey(x => new { x.PostedDocumentId, x.TenantId })
                    .HasPrincipalKey(x => new { x.Id, x.TenantId })
                    .HasConstraintName("FK_StockCount_PostedDocument");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.CountNumber })
            .IsUnique()
            .HasDatabaseName("UX_StockCount_TenantId_WarehouseId_CountNumber");

        b.ToTable("StockCount", t =>
        {
            t.HasCheckConstraint("CK_StockCount_Status", "[Status] IN (1,2,3,4,5)");
            t.HasCheckConstraint("CK_StockCount_ScopeType", "[ScopeType] IN (1,2,3,4)");
        });
    }
}

internal sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> b)
    {
        b.ToTable("StockCountLine");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExpectedQuantity).Money_().IsRequired();
        b.Property(x => x.CountedQuantity).Money_();
        b.Property(x => x.VarianceQuantity).Money_();
        b.Property(x => x.UnitCost).Money_();
        b.Property(x => x.Resolution).Enum_().IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Count)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => new { x.StockCountId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockCountLine_StockCount");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockCountLine_Product");

        b.HasOne<Batch>()
            .WithMany()
            .HasForeignKey(x => new { x.BatchId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockCountLine_Batch");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_StockCountLine_Unit");

        // Both are required (ADR-0041): without the filtered variant two count lines
        // for the same non-batched product would both be allowed.
        b.HasIndex(x => new { x.TenantId, x.StockCountId, x.ProductId, x.BatchId })
            .IsUnique()
            .HasDatabaseName("UX_StockCountLine_TenantId_StockCountId_ProductId_BatchId");

        b.HasIndex(x => new { x.TenantId, x.StockCountId, x.ProductId })
            .IsUnique()
            .HasFilter("[BatchId] IS NULL")
            .HasDatabaseName("UX_StockCountLine_TenantId_StockCountId_ProductId_NoBatch");

        b.ToTable("StockCountLine", t =>
        {
            t.HasCheckConstraint("CK_StockCountLine_Resolution", "[Resolution] IN (1,2,3,4)");
            t.HasCheckConstraint(
                "CK_StockCountLine_Variance",
                "[VarianceQuantity] IS NULL OR [VarianceQuantity] = [CountedQuantity] - [ExpectedQuantity]");
        });
    }
}