using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Events;
using Stockly.Domain.Modules.Files;
using Stockly.Domain.Modules.Food;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Inventory;
using Stockly.Domain.Modules.Master;

namespace Stockly.Infrastructure.Persistence.Configurations;

// ---------------------------------------------------------------------------
// events
//
// Every tenant-scoped parent is referenced by a COMPOSITE (Id, TenantId)
// foreign key (DATABASE_DESIGN §5.1 rule 1). The catalogue abbreviates some of
// these to "FK -> food.MealType", but a single-column FK would accept a row
// pointing at another tenant's meal type. The composite key is the stronger
// reading of the same rule and costs nothing.
// ---------------------------------------------------------------------------

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.ToTable("Event");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Description).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.Type).Enum_().IsRequired();
        b.Property(x => x.StartAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.EndAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.DurationDays).IsRequired();
        b.Property(x => x.ExpectedAttendees).IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.MealPlanNotes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.BudgetAmount).Money_();
        b.Property(x => x.RowVersion).IsRowVersion();

        // No CostSnapshotId FK (ADR-0037): the snapshot row is written after the
        // event is costed, so a pointer would be permanently null or force an
        // impossible write order. "Current" = newest EventFoodCostSnapshot.

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Event_Warehouse");

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Event_TenantId_Code");

        b.HasIndex(x => new { x.TenantId, x.Status, x.StartAtUtc })
            .HasDatabaseName("IX_Event_TenantId_Status_StartAtUtc");

        b.ToTable("Event", t =>
        {
            t.HasCheckConstraint("CK_Event_Type", "[Type] IN (1,2,3,4,5)");
            t.HasCheckConstraint("CK_Event_Status", "[Status] IN (1,2,3,4,5)");
            t.HasCheckConstraint("CK_Event_EndAtUtc", "[EndAtUtc] > [StartAtUtc]");
            t.HasCheckConstraint("CK_Event_DurationDays", "[DurationDays] > 0");
            t.HasCheckConstraint("CK_Event_ExpectedAttendees", "[ExpectedAttendees] >= 0");
        });
    }
}

internal sealed class EventRequirementConfiguration : IEntityTypeConfiguration<EventRequirement>
{
    public void Configure(EntityTypeBuilder<EventRequirement> b)
    {
        b.ToTable("EventRequirement");
        b.HasKey(x => x.Id);
        b.Property(x => x.BasisType).Enum_().IsRequired();
        b.Property(x => x.QuantityPerBasisUnit).Money_();
        b.Property(x => x.TotalRequiredQuantity).Money_().IsRequired();
        b.Property(x => x.IsCritical).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<Event>()
            .WithMany(x => x.Requirements)
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventRequirement_Event");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventRequirement_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventRequirement_Unit");

        b.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(x => new { x.MealTypeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventRequirement_MealType");

        // Both uniques are required (ADR-0041): without the filtered variant two
        // event-wide requirements for the same product would both be allowed,
        // because SQL Server treats NULLs as equal only inside an IS NULL filter.
        b.HasIndex(x => new { x.TenantId, x.EventId, x.ProductId, x.MealTypeId })
            .IsUnique()
            .HasDatabaseName("UX_EventRequirement_TenantId_EventId_ProductId_MealTypeId");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.ProductId })
            .IsUnique()
            .HasFilter("[MealTypeId] IS NULL")
            .HasDatabaseName("UX_EventRequirement_TenantId_EventId_ProductId_NoMealType");

        b.ToTable("EventRequirement", t =>
        {
            t.HasCheckConstraint("CK_EventRequirement_BasisType", "[BasisType] IN (1,2,3,4)");
            t.HasCheckConstraint(
                "CK_EventRequirement_QuantityPerBasisUnit",
                "[BasisType] = 1 OR [QuantityPerBasisUnit] IS NOT NULL");
            t.HasCheckConstraint("CK_EventRequirement_TotalRequiredQuantity", "[TotalRequiredQuantity] >= 0");
        });
    }
}

internal sealed class EventConsumptionConfiguration : IEntityTypeConfiguration<EventConsumption>
{
    public void Configure(EntityTypeBuilder<EventConsumption> b)
    {
        b.ToTable("EventConsumption");
        b.HasKey(x => x.Id);
        b.Property(x => x.PlannedQuantity).Money_().IsRequired();
        b.Property(x => x.ConsumedQuantity).Money_().IsRequired();
        b.Property(x => x.ActualUnitCost).Money_();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<Event>()
            .WithMany(x => x.Consumptions)
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_Event");

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_Warehouse");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_Product");

        b.HasOne<Batch>()
            .WithMany()
            .HasForeignKey(x => new { x.BatchId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_Batch");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_Unit");

        b.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(x => new { x.MealTypeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_MealType");

        // Proof of actual consumption (ADR-0038): without this FK a document id
        // pointing at nothing would pass validation and break traceability.
        b.HasOne<StockDocument>()
            .WithMany()
            .HasForeignKey(x => new { x.StockDocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventConsumption_StockDocument");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.ProductId, x.MealTypeId })
            .IsUnique()
            .HasDatabaseName("UX_EventConsumption_TenantId_EventId_ProductId_MealTypeId");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.ProductId })
            .IsUnique()
            .HasFilter("[MealTypeId] IS NULL")
            .HasDatabaseName("UX_EventConsumption_TenantId_EventId_ProductId_NoMealType");

        b.ToTable("EventConsumption", t =>
        {
            t.HasCheckConstraint("CK_EventConsumption_PlannedQuantity", "[PlannedQuantity] >= 0");
            t.HasCheckConstraint("CK_EventConsumption_ConsumedQuantity", "[ConsumedQuantity] >= 0");

            // DELIBERATELY ABSENT (ADR-0039): there is no
            // CHECK (ConsumedQuantity <= PlannedQuantity * 2). Over-consumption is a
            // legitimate signal, so the sanity guard is an application rule with a
            // configurable multiplier, an override permission and an audit record.
        });
    }
}

internal sealed class EventAttendanceConfiguration : IEntityTypeConfiguration<EventAttendance>
{
    public void Configure(EntityTypeBuilder<EventAttendance> b)
    {
        b.ToTable("EventAttendance");
        b.HasKey(x => x.Id);
        b.Property(x => x.GuestName).HasMaxLength(Convention.NameLength);
        b.Property(x => x.IsAttending).IsRequired();
        b.Property(x => x.DaysAttending).IsRequired();
        b.Property(x => x.MealsPerDay).HasColumnType(Convention.MealsPerDay);
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<Event>()
            .WithMany(x => x.Attendances)
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventAttendance_Event");

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.MembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventAttendance_TenantMembership");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.MembershipId })
            .IsUnique()
            .HasFilter("[MembershipId] IS NOT NULL")
            .HasDatabaseName("UX_EventAttendance_TenantId_EventId_MembershipId");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.GuestName })
            .IsUnique()
            .HasFilter("[MembershipId] IS NULL")
            .HasDatabaseName("UX_EventAttendance_TenantId_EventId_GuestName");

        b.ToTable("EventAttendance", t =>
        {
            t.HasCheckConstraint("CK_EventAttendance_DaysAttending", "[DaysAttending] > 0");
            t.HasCheckConstraint("CK_EventAttendance_MealsPerDay", "[MealsPerDay] IS NULL OR [MealsPerDay] >= 0");

            // A row must be attributable: either a Stockly membership or a guest name.
            t.HasCheckConstraint(
                "CK_EventAttendance_Attributable",
                "[MembershipId] IS NOT NULL OR [GuestName] IS NOT NULL");
        });
    }
}

internal sealed class EventShortageSnapshotConfiguration : IEntityTypeConfiguration<EventShortageSnapshot>
{
    public void Configure(EntityTypeBuilder<EventShortageSnapshot> b)
    {
        b.ToTable("EventShortageSnapshot");
        b.HasKey(x => x.Id);

        // Append-only (ADR-0037): no RowVersion, no IsDeleted, no update. The
        // current shortage for an event/product is the newest row.

        b.Property(x => x.CalculatedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.RequiredQuantity).Money_().IsRequired();
        b.Property(x => x.OnHandQuantity).Money_().IsRequired();
        b.Property(x => x.IncomingQuantity).Money_().IsRequired();
        b.Property(x => x.ShortageQuantity).Money_().IsRequired();
        b.Property(x => x.RecommendedPurchaseQuantity).Money_().IsRequired();
        b.Property(x => x.UnitCost).Money_();
        b.Property(x => x.EstimatedCost).Money_();

        b.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventShortageSnapshot_Event");

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventShortageSnapshot_Warehouse");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventShortageSnapshot_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventShortageSnapshot_Unit");

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.CreatedByMembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventShortageSnapshot_TenantMembership");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.CalculatedAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("UX_EventShortageSnapshot_TenantId_EventId_CalculatedAtUtc");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.ProductId })
            .HasDatabaseName("IX_EventShortageSnapshot_TenantId_EventId_ProductId");

        b.ToTable("EventShortageSnapshot", t =>
        {
            t.HasCheckConstraint("CK_EventShortageSnapshot_RequiredQuantity", "[RequiredQuantity] >= 0");
            t.HasCheckConstraint("CK_EventShortageSnapshot_ShortageQuantity", "[ShortageQuantity] >= 0");
            t.HasCheckConstraint(
                "CK_EventShortageSnapshot_RecommendedPurchaseQuantity",
                "[RecommendedPurchaseQuantity] >= 0");
        });
    }
}

// ---------------------------------------------------------------------------
// food
// ---------------------------------------------------------------------------

internal sealed class MealTypeConfiguration : IEntityTypeConfiguration<MealType>
{
    public void Configure(EntityTypeBuilder<MealType> b)
    {
        b.ToTable("MealType");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.DefaultServingsPerDay).Percent_();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_MealType_TenantId_Code");
    }
}

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> b)
    {
        b.ToTable("Recipe");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Description).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.YieldQuantity).Money_().IsRequired();
        b.Property(x => x.ServingsPerYield).HasColumnType(Convention.ServingsRatio);
        b.Property(x => x.Instructions).HasColumnType("nvarchar(max)");
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(x => new { x.MealTypeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Recipe_MealType");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.YieldUnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Recipe_Unit");

        b.HasOne<Attachment>()
            .WithMany()
            .HasForeignKey(x => new { x.ImageAttachmentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Recipe_Attachment");

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Recipe_TenantId_Code");

        b.ToTable("Recipe", t =>
            t.HasCheckConstraint("CK_Recipe_YieldQuantity", "[YieldQuantity] > 0"));
    }
}

internal sealed class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> b)
    {
        b.ToTable("RecipeItem");
        b.HasKey(x => x.Id);
        b.Property(x => x.LineNumber).IsRequired();
        b.Property(x => x.QuantityPerYield).Money_().IsRequired();
        b.Property(x => x.WastagePercent).Percent_().IsRequired();
        b.Property(x => x.IsOptional).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Recipe)
            .WithMany(x => x.Items)
            .HasForeignKey(x => new { x.RecipeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RecipeItem_Recipe");

        b.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RecipeItem_Product");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.UnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RecipeItem_Unit");

        b.HasIndex(x => new { x.RecipeId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("UX_RecipeItem_RecipeId_LineNumber");

        b.ToTable("RecipeItem", t =>
        {
            t.HasCheckConstraint("CK_RecipeItem_QuantityPerYield", "[QuantityPerYield] > 0");
            t.HasCheckConstraint("CK_RecipeItem_WastagePercent", "[WastagePercent] BETWEEN 0 AND 100");
        });
    }
}

internal sealed class RecipeCostSnapshotConfiguration : IEntityTypeConfiguration<RecipeCostSnapshot>
{
    public void Configure(EntityTypeBuilder<RecipeCostSnapshot> b)
    {
        b.ToTable("RecipeCostSnapshot");
        b.HasKey(x => x.Id);

        // Append-only (ADR-0037): no RowVersion, no update, no delete. The current
        // recipe cost is the newest row.

        b.Property(x => x.CalculatedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.TotalCost).Money_().IsRequired();
        b.Property(x => x.YieldQuantity).Money_().IsRequired();
        b.Property(x => x.CostPerYieldUnit).Money_().IsRequired();
        b.Property(x => x.ServingsPerYield).HasColumnType(Convention.ServingsRatio);
        b.Property(x => x.CostPerServing).Money_();
        b.Property(x => x.CostSourceSummary).HasMaxLength(500);
        b.Property(x => x.ValuationMethod).Enum_().IsRequired();
        b.Property(x => x.LineCosts).HasColumnType("nvarchar(max)");

        b.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(x => new { x.RecipeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RecipeCostSnapshot_Recipe");

        b.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => new { x.YieldUnitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RecipeCostSnapshot_Unit");

        b.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.CreatedByMembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RecipeCostSnapshot_TenantMembership");

        b.HasIndex(x => new { x.TenantId, x.RecipeId, x.CalculatedAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_RecipeCostSnapshot_TenantId_RecipeId_CalculatedAtUtc");

        b.ToTable("RecipeCostSnapshot", t =>
        {
            t.HasCheckConstraint("CK_RecipeCostSnapshot_TotalCost", "[TotalCost] >= 0");
            t.HasCheckConstraint("CK_RecipeCostSnapshot_YieldQuantity", "[YieldQuantity] > 0");
            t.HasCheckConstraint("CK_RecipeCostSnapshot_ValuationMethod", "[ValuationMethod] IN (1,2,3)");
        });
    }
}

internal sealed class EventFoodCostSnapshotConfiguration : IEntityTypeConfiguration<EventFoodCostSnapshot>
{
    public void Configure(EntityTypeBuilder<EventFoodCostSnapshot> b)
    {
        b.ToTable("EventFoodCostSnapshot");
        b.HasKey(x => x.Id);

        // Append-only (ADR-0037). There is deliberately no IsCurrent flag: "current"
        // is the newest row per (TenantId, EventId, MealTypeId). A mutable flag on an
        // append-only table means two writers can disagree about which row is current.

        b.Property(x => x.CalculatedAtUtc).HasPrecision(3).IsRequired();
        b.Property(x => x.TotalServings).IsRequired();
        b.Property(x => x.TotalCost).Money_().IsRequired();
        b.Property(x => x.CostPerServing).Money_();
        b.Property(x => x.CostPerAttendee).Money_();
        b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsFixedLength().IsRequired();
        b.Property(x => x.Breakdown).HasColumnType("nvarchar(max)");

        b.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => new { x.EventId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventFoodCostSnapshot_Event");

        b.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(x => new { x.MealTypeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EventFoodCostSnapshot_MealType");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.CalculatedAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_EventFoodCostSnapshot_TenantId_EventId_CalculatedAtUtc");

        // Both are required (ADR-0041): one recalculation must not be able to insert
        // two rows for the same event/meal/time.
        b.HasIndex(x => new { x.TenantId, x.EventId, x.CalculatedAtUtc, x.MealTypeId })
            .IsUnique()
            .HasDatabaseName("UX_EventFoodCostSnapshot_TenantId_EventId_CalculatedAtUtc_MealTypeId");

        b.HasIndex(x => new { x.TenantId, x.EventId, x.CalculatedAtUtc })
            .IsUnique()
            .HasFilter("[MealTypeId] IS NULL")
            .HasDatabaseName("UX_EventFoodCostSnapshot_TenantId_EventId_CalculatedAtUtc_NoMealType");

        b.ToTable("EventFoodCostSnapshot", t =>
        {
            t.HasCheckConstraint("CK_EventFoodCostSnapshot_TotalServings", "[TotalServings] >= 0");
            t.HasCheckConstraint("CK_EventFoodCostSnapshot_TotalCost", "[TotalCost] >= 0");
        });
    }
}