using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Master;
using Stockly.Domain.Modules.Purchasing;

namespace Stockly.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Category");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // Self-reference, composite on (Id, TenantId): a category may not be
        // re-parented into another tenant's tree. Cycle prevention is a domain rule
        // with a tested depth limit of 3, not a database constraint.
        b.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => new { x.ParentCategoryId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Category_ParentCategory");

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Category_TenantId_Code");

        b.HasIndex(x => new { x.TenantId, x.ParentCategoryId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Category_TenantId_ParentCategoryId_Code");
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> b)
    {
        b.ToTable("Unit");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.UnitType).Enum_().IsRequired();
        b.Property(x => x.DecimalPlaces).IsRequired();
        b.Property(x => x.IsBaseUnit).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Unit_TenantId_Code");

        b.ToTable("Unit", t =>
        {
            t.HasCheckConstraint("CK_Unit_UnitType", "[UnitType] IN (1,2,3,4,5)");
            t.HasCheckConstraint("CK_Unit_DecimalPlaces", "[DecimalPlaces] <= 4");
        });
    }
}

internal sealed class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> b)
    {
        b.ToTable("UnitConversion");
        b.HasKey(x => x.Id);
        b.Property(x => x.Factor).Ratio_().IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_UnitConversion_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.FromUnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_UnitConversion_FromUnit");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.ToUnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_UnitConversion_ToUnit");

        // Two filtered uniques are both REQUIRED (ADR-0041): SQL Server treats NULLs as
        // equal only inside an IS NULL-filtered index, so without the second a tenant
        // could hold two tenant-wide defaults for the same unit pair.
        b.HasIndex(x => new { x.TenantId, x.ProductId, x.FromUnitId, x.ToUnitId })
            .IsUnique()
            .HasFilter("[ProductId] IS NOT NULL")
            .HasDatabaseName("UX_UnitConversion_Product");

        b.HasIndex(x => new { x.TenantId, x.FromUnitId, x.ToUnitId })
            .IsUnique()
            .HasFilter("[ProductId] IS NULL")
            .HasDatabaseName("UX_UnitConversion_TenantDefault");

        b.ToTable("UnitConversion", t =>
        {
            t.HasCheckConstraint("CK_UnitConversion_Factor", "[Factor] > 0");
            t.HasCheckConstraint("CK_UnitConversion_DistinctUnits", "[FromUnitId] <> [ToUnitId]");
        });
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Product");
        b.HasKey(x => x.Id);
        b.Property(x => x.Sku).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Description).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.IsBatchTracked).IsRequired();
        b.Property(x => x.IsExpiryTracked).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => new { x.CategoryId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Product_Category");

        b.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => new { x.BaseUnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Product_BaseUnit");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.PurchaseUnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Product_PurchaseUnit");

        b.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.DefaultReasonCodeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Product_DefaultReasonCode");

        b.HasIndex(x => new { x.TenantId, x.Sku })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Product_TenantId_Sku");

        b.HasIndex(x => new { x.TenantId, x.CategoryId, x.IsActive }).HasDatabaseName("IX_Product_TenantId_CategoryId_IsActive");
        b.HasIndex(x => new { x.TenantId, x.Name }).HasDatabaseName("IX_Product_TenantId_Name");

        b.ToTable("Product", t =>
            t.HasCheckConstraint(
                "CK_Product_ExpiryTracked",
                "[IsExpiryTracked] = 0 OR [ShelfLifeDays] IS NOT NULL"));
    }
}

internal sealed class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> b)
    {
        b.ToTable("ProductBarcode");
        b.HasKey(x => x.Id);
        b.Property(x => x.Barcode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Symbology).Enum_().IsRequired();
        b.Property(x => x.IsPrimary).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Product)
            .WithMany(x => x.Barcodes)
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductBarcode_Product");

        // Unfiltered: Barcode is NOT NULL on this table, so a filtered variant would
        // be pointless (ADR-0041 does not apply here).
        b.HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique().HasDatabaseName("UX_ProductBarcode_TenantId_Barcode");

        // ADR-0033: "primary" is a row, not a second column on Product, so two
        // barcodes can never both claim to be primary for one product.
        b.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1")
            .HasDatabaseName("UX_ProductBarcode_ProductId_Primary");

        b.ToTable("ProductBarcode", t =>
            t.HasCheckConstraint("CK_ProductBarcode_Symbology", "[Symbology] IN (1,2,3,4,5,6)"));
    }
}

internal sealed class ProductWarehouseSettingConfiguration : IEntityTypeConfiguration<ProductWarehouseSetting>
{
    public void Configure(EntityTypeBuilder<ProductWarehouseSetting> b)
    {
        b.ToTable("ProductWarehouseSetting");
        b.HasKey(x => x.Id);
        b.Property(x => x.MinimumQuantity).Money_();
        b.Property(x => x.MaximumQuantity).Money_();
        b.Property(x => x.ReorderQuantity).Money_();
        b.Property(x => x.IsPreferred).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductWarehouseSetting_Warehouse");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductWarehouseSetting_Product");

        b.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(x => new { x.DefaultSupplierId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_ProductWarehouseSetting_DefaultSupplier");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_ProductWarehouseSetting_TenantId_WarehouseId_ProductId");

        b.ToTable("ProductWarehouseSetting", t =>
            t.HasCheckConstraint(
                "CK_ProductWarehouseSetting_QuantityRange",
                "[MaximumQuantity] IS NULL OR [MinimumQuantity] IS NULL OR [MaximumQuantity] >= [MinimumQuantity]"));
    }
}

internal sealed class ReasonCodeConfiguration : IEntityTypeConfiguration<ReasonCode>
{
    public void Configure(EntityTypeBuilder<ReasonCode> b)
    {
        b.ToTable("ReasonCode");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.MovementType).Enum_().IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // MovementType leads the tenant-scoped uniqueness so the same short code can
        // exist for, say, both Waste and Adjustment without colliding.
        b.HasIndex(x => new { x.TenantId, x.MovementType, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_ReasonCode_TenantId_MovementType_Code");

        b.ToTable("ReasonCode", t =>
            t.HasCheckConstraint("CK_ReasonCode_MovementType", "[MovementType] IN (1,2,3,4,5,6,7)"));
    }
}