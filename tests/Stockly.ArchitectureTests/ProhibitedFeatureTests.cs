using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Stockly.ArchitectureTests;

/// <summary>
/// Absence tests for the five owner decisions taken on 2026-09-30
/// (<c>TESTING_STRATEGY.md</c> §3.2).
/// <para>
/// Each of those decisions is a <em>prohibition</em>. A prohibition cannot be
/// verified by reading a list of what exists, so each is asserted by reflection
/// over the entity model: the test fails if the prohibited thing is ever built.
/// A failure here is a <strong>build failure</strong> — it means a feature V1
/// explicitly excludes has been implemented.
/// </para>
/// <para>
/// These need no database: they inspect the CLR types and the generated model
/// snapshot, not a live server.
/// </para>
/// </summary>
public sealed class ProhibitedFeatureTests
{
    private static readonly Assembly Domain = typeof(Stockly.Domain.Common.Entity).Assembly;

    private static IReadOnlyList<Type> Entities =>
        Domain.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Namespace?.StartsWith("Stockly.Domain.Modules.", StringComparison.Ordinal) == true)
            .ToArray();

    private static IReadOnlyList<Type> InventoryEntities =>
        [.. Entities.Where(type => type.Namespace!.Contains(".Inventory", StringComparison.Ordinal))];

    private static IReadOnlyList<(string Name, Type Type, Type PropertyType)> AllProperties =>
    [
        .. Entities
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(property => (property.Name, property.DeclaringType!, property.PropertyType)),
    ];

    // ---- ADR-0045 — no location scope in V1 -------------------------------

    [Fact]
    public void No_stock_entity_has_a_location_bin_aisle_rack_or_zone_property()
    {
        // Matched precisely: either the bare noun (`Location`, `Bin`, …) or a key
        // ending in it (`LocationId`, `BinId`). A loose substring rule would also
        // match `ShelfLifeDays`, which is an expiry field on master.Product and has
        // nothing to do with where stock sits — an alert-fatigue control is not a
        // control.
        string[] bareNouns = ["Location", "Bin", "Aisle", "Rack", "Zone", "Shelf"];

        static bool IsProhibited(string propertyName, string[] nouns) =>
            nouns.Any(noun =>
                propertyName.Equals(noun, StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals(noun + "Id", StringComparison.OrdinalIgnoreCase));

        var offenders = AllProperties
            .Where(property => IsProhibited(property.Name, bareNouns))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0045: WarehouseId is the finest stock scope in V1 and V1.1. " +
            "Descriptive sub-locations are V1.1 labels on a warehouse that owns no stock.");
    }

    [Fact]
    public void Shelf_life_is_not_a_location()
    {
        // Guards the test above against being "fixed" by widening the rule until it
        // rejects legitimate fields. Expiry tracking is a product attribute.
        var offenders = AllProperties
            .Where(property => property.Name.Contains("Shelf", StringComparison.OrdinalIgnoreCase))
            .Where(property => property.PropertyType == typeof(int?)
                || property.PropertyType == typeof(int))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().OnlyContain(name => name.Contains("ShelfLife", StringComparison.Ordinal));
    }

    [Fact]
    public void Warehouse_has_no_parent_or_navigation_to_another_warehouse()
    {
        var warehouse = Entities.Single(t => t.Name == "Warehouse");

        var offenders = warehouse.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(Guid?)
                && property.Name.EndsWith("WarehouseId", StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0045: there is no warehouse hierarchy, so a caller cannot escalate by " +
            "naming a deeper scope.");
    }

    [Fact]
    public void No_entity_is_named_like_a_location_or_bin()
    {
        string[] prohibited = ["Location", "Bin", "Aisle", "Rack", "Shelf", "Zone"];

        var offenders = Entities
            .Where(type => prohibited.Any(
                word => type.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(type => type.Name)
            .ToArray();

        offenders.Should().BeEmpty("ADR-0045: no V1 table may acquire a location or bin key.");
    }

    // ---- ADR-0046 — single currency, no FX -------------------------------

    [Fact]
    public void No_entity_has_an_exchange_rate_or_converted_amount_property()
    {
        string[] prohibited =
        [
            "ExchangeRate", "RateDate", "ConvertedAmount", "FxRate", "BaseAmount",
            "TargetAmount", "RateTo", "RateFrom",
        ];

        var offenders = AllProperties
            .Where(property => prohibited.Any(
                word => property.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().BeEmpty("ADR-0046: one tenant operates in one currency; V1 has no FX.");
    }

    /// <summary>
    /// ADR-0046 makes every <c>CurrencyCode</c> column <em>informational</em>: there
    /// is one currency per tenant and nothing converts between them. So the absence
    /// to assert is any <em>conversion</em> path, not any currency column — the
    /// ratified design deliberately records the currency on <c>Supplier</c>,
    /// <c>PurchaseOrder</c> and <c>EventFoodCostSnapshot</c> as a fact about the
    /// transaction as it was.
    /// </summary>
    [Fact]
    public void Currency_columns_are_informational_and_never_conversion_paths()
    {
        // Every currency-bearing property must be a `CurrencyCode`. Anything else
        // bearing "Currency" would be a second amount column to convert.
        var offenders = AllProperties
            .Where(property => property.Name.Contains("Currency", StringComparison.OrdinalIgnoreCase))
            .Where(property => !property.Name.Equals("CurrencyCode", StringComparison.Ordinal))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0046: `CurrencyCode` is informational. Any other currency-bearing property " +
            "would be a conversion column, which V1 must not have.");
    }

    [Theory]
    [InlineData("ExchangeRate")]
    [InlineData("ExchangeRateDate")]
    [InlineData("FxRate")]
    [InlineData("RateToCurrency")]
    [InlineData("RateFromCurrency")]
    [InlineData("ConvertedAmount")]
    [InlineData("ConvertedTotal")]
    [InlineData("BaseAmount")]
    [InlineData("TargetAmount")]
    public void No_fx_property_exists(string propertyName)
    {
        AllProperties
            .Where(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty("ADR-0046: V1 has no FX and no conversion column.");
    }

    [Fact]
    public void There_is_no_fx_or_currency_conversion_table()
    {
        var offenders = Entities
            .Where(type =>
                type.Name.Equals("ExchangeRate", StringComparison.OrdinalIgnoreCase)
                || type.Name.Equals("FxRate", StringComparison.OrdinalIgnoreCase)
                || type.Name.Equals("CurrencyConversion", StringComparison.OrdinalIgnoreCase)
                || type.Name.StartsWith("Currency", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.Name)
            .ToArray();

        offenders.Should().BeEmpty("ADR-0046: no FX table, no rate table, no conversion table.");
    }

    [Fact]
    public void Unit_and_purchase_order_conversion_are_not_currency_and_stay()
    {
        // Guards the two tests above against being "fixed" by banning the words
        // "Conversion" and "Converted" outright. Both of these are legitimate,
        // documented V1 fields and ADR-0046 does not touch them:
        //   master.UnitConversion      — a mass/volume ratio
        //   ConvertedPurchaseOrderId    — purchasing-order document state
        Entities
            .Where(type => type.Name.Contains("Conversion", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.Name)
            .Should().BeEquivalentTo(["UnitConversion"], "a mass/volume ratio, not a currency rate");

        AllProperties
            .Where(property => property.Name.Contains("Converted", StringComparison.OrdinalIgnoreCase))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .Should().BeEquivalentTo(
            [
                "PurchaseRequest.ConvertedPurchaseOrderId",
            ],
            "document state: the purchase order a request was converted into");
    }

    // ---- ADR-0047 — no in-transit transfer state --------------------------

    [Fact]
    public void No_entity_has_an_in_transit_or_receiving_transfer_state()
    {
        string[] prohibited = ["InTransit", "InTransfer", "TransferState", "ReceivingState", "ShippedState"];

        var offenders = AllProperties
            .Where(property => prohibited.Any(
                word => property.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0047: a transfer is atomic across both warehouses. There is deliberately " +
            "no in-transit state that could hold stock, so a failure between the two legs " +
            "cannot strand a balance.");
    }

    [Fact]
    public void Warehouse_type_has_no_in_transit_value()
    {
        // ADR-0047. A Warehouse.Type of "InTransit" would reintroduce the third
        // location that the atomic transfer removes.
        var warehouseType = Domain.GetType("Stockly.Domain.Common.WarehouseType");

        warehouseType.Should().NotBeNull();
        Enum.GetNames(warehouseType!)
            .Should().NotContain(name => name.Contains("Transit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void There_is_no_transfer_shipment_or_transfer_receipt_entity()
    {
        string[] prohibited = ["TransferShipment", "TransferReceipt", "Shipment", "Transit"];

        var offenders = Entities
            .Where(type => prohibited.Any(
                word => type.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(type => type.Name)
            .ToArray();

        offenders.Should().BeEmpty("ADR-0047: a transfer is one atomic operation, not two documents.");
    }

    // ---- ADR-0048 — no legal-entity layer ---------------------------------

    [Fact]
    public void There_is_no_legal_entity_entity()
    {
        var offenders = Entities
            .Where(type => type.Name.Contains("LegalEntity", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.Name)
            .ToArray();

        offenders.Should().BeEmpty("ADR-0048: one Tenant is one legal entity.");
    }

    [Fact]
    public void No_entity_references_a_tenant_through_anything_but_its_own_TenantId()
    {
        // A property such as `ParentTenantId` or `CounterTenantId` would be a second
        // tenant reference and therefore a cross-entity path that the mandatory
        // authorization chain does not cover.
        var offenders = AllProperties
            .Where(property => property.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .Where(property => property.Name != "TenantId")
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0048: a request addresses exactly one legal entity, so the tenant step " +
            "of the chain cannot be satisfied partially.");
    }

    [Fact]
    public void Tenant_root_has_no_TenantId_of_its_own()
    {
        // This is the structural half of ADR-0048 and the single root exception in
        // the tenant-key conventions: the tenant root cannot be scoped to a tenant.
        var tenant = Entities.Single(t => t.Name == "Tenant");

        tenant.GetProperty("TenantId").Should().BeNull();
    }

    // ---- ADR-0050 — no V1 stock reservations ------------------------------

    [Fact]
    public void There_is_no_stock_reservation_entity_or_table()
    {
        var offenders = Entities
            .Where(type => type.Name.Contains("Reservation", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.Name)
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0050 / TBD-26: hard vs soft reservations, release timing and lifecycle " +
            "are all undecided. ReservedQuantity being 0 in V1 is not a soft-reservation design.");
    }

    /// <summary>
    /// ADR-0050 leaves reservations undecided, so the only permitted trace of them
    /// is the always-zero quantity the design puts on the two balance tables. A
    /// reservation table, or a reserved quantity anywhere else, would be an
    /// implementation of the undecided decision.
    /// </summary>
    [Fact]
    public void Reserved_quantity_exists_only_on_the_two_balance_tables()
    {
        var offenders = AllProperties
            .Where(property => property.Name.Contains("Reserved", StringComparison.OrdinalIgnoreCase))
            .Select(property => $"{property.Type.Name}.{property.Name}")
            .ToArray();

        offenders.Should().BeEquivalentTo(
            ["InventoryLot.ReservedQuantity", "StockBalance.ReservedQuantity"],
            "DATABASE_DESIGN.md §4.5 puts ReservedQuantity on StockBalance and " +
            "InventoryLot, always 0 in V1, and nowhere else");
    }

    [Fact]
    public void The_inventory_entities_are_exactly_the_ratified_eight()
    {
        // ADR-0045/0047/0050 all add or forbid inventory concepts. Pinning the set
        // makes any addition a deliberate act rather than an accident.
        InventoryEntities
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Should().BeEquivalentTo(
            [
                "Batch", "InventoryLot",
                "StockBalance",
                "StockDocument", "StockDocumentLine", "StockTransaction",
                "StockCount", "StockCountLine",
            ]);
    }
}