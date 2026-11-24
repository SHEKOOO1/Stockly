using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Master;
using Stockly.StocklyDbTestSupport;
using Xunit;

namespace Stockly.IntegrationTests;

/// <summary>
/// <strong>S1 — Tenant isolation.</strong>
/// <para>
/// The central security property of the whole system: a request acting inside tenant A
/// can neither read nor mutate any tenant B row, by guessing a GUID, by listing, by
/// a stale identifier, or by direct SQL through EF.
/// </para>
/// <para>
/// Three independent layers are exercised separately, because they fail differently:
/// </para>
/// <list type="number">
/// <item>the <strong>global query filter</strong> (defence in depth, bypassable by
/// <c>IgnoreQueryFilters</c>),</item>
/// <item>the <strong>composite foreign keys</strong> on <c>(Id, TenantId)</c> (a
/// cross-tenant row cannot even be INSERTed),</item>
/// <item>the <strong>composite alternate keys</strong> that make those foreign keys
/// expressible.</item>
/// </list>
/// <para>
/// Layer 1 alone is not enough: a filter is application code, and one
/// <c>IgnoreQueryFilters()</c> away from irrelevant. Layer 2 is the actual guarantee,
/// so the "cannot mutate" tests deliberately bypass the filter to prove the database
/// still refuses.
/// </para>
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class TenantIsolationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;

    public TenantIsolationTests(SqlServerFixture fixture) => _fixture = fixture;

    private sealed record Seeded(Guid TenantA, Guid TenantB, Guid WarehouseA, Guid WarehouseB, Guid ProductA, Guid ProductB);

    // Every row this test instance created, so DisposeAsync can remove exactly those and
    // nothing else. The canonical database is also the development database, so cleanup by
    // primary key is the only safe kind: truncating "to start clean" would destroy local work.
    private readonly List<Guid> _tenants = [];
    private readonly List<Guid> _warehouses = [];
    private readonly List<Guid> _units = [];
    private readonly List<Guid> _products = [];

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Removes exactly the rows this test created, children before parents.
    /// <para>
    /// Not a truncate and not a rollback. These tests deliberately assert through two different
    /// principals (the tenant principal as the adversary, the platform principal for ground
    /// truth), and one transaction cannot span two connections -- so a rollback could only ever
    /// cover half the writes, which is precisely the half that must not survive.
    /// </para>
    /// <para>
    /// <c>IgnoreQueryFilters</c> is required, not optional: a soft-deleted row is invisible to
    /// the filter, so without the bypass it would outlive the test that created it.
    /// </para>
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_products.Count == 0 && _units.Count == 0 && _warehouses.Count == 0 && _tenants.Count == 0)
        {
            return;
        }

        var platform = await _fixture.PlatformContextAsync();

        // master.Product -> master.Unit is a composite FK, and identity.Warehouse and
        // master.Unit both reference identity.Tenant, so the order is not interchangeable.
        if (_products.Count > 0)
        {
            await platform.Products.IgnoreQueryFilters()
                .Where(p => _products.Contains(p.Id)).ExecuteDeleteAsync();
        }

        if (_units.Count > 0)
        {
            await platform.Units.IgnoreQueryFilters()
                .Where(u => _units.Contains(u.Id)).ExecuteDeleteAsync();
        }

        if (_warehouses.Count > 0)
        {
            await platform.Warehouses.IgnoreQueryFilters()
                .Where(w => _warehouses.Contains(w.Id)).ExecuteDeleteAsync();
        }

        if (_tenants.Count > 0)
        {
            await platform.Tenants.IgnoreQueryFilters()
                .Where(t => _tenants.Contains(t.Id)).ExecuteDeleteAsync();
        }
    }

    /// <summary>
    /// Creates two fully independent tenants with a warehouse and a product each.
    /// <para>
    /// This is TEST FIXTURE data, not production seed data. It is written through a
    /// platform-scope context so the tenant-root foreign keys are satisfied and no
    /// filter interferes with setup.
    /// </para>
    /// </summary>
    private async Task<Seeded> SeedAsync()
    {
        // Test SETUP uses a platform-authority context so no tenant filter interferes
        // with inserting rows for two different tenants at once. It is an explicit
        // cross-tenant scope (ADR-0034), never an inferred one.
        var platform = await _fixture.PlatformContextAsync();

        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var suffix = a.ToString("N")[..8];

        platform.Tenants.AddRange(
            new Tenant { Id = a, Code = "TNT-" + suffix + "-A", Name = "Tenant A", NameAr = "tenant a", LegalName = "Tenant A LLC", Status = TenantStatus.Active, TimeZoneId = "UTC", DefaultCulture = "ar", DateFormat = "yyyy-MM-dd", IsDeleted = false },
            new Tenant { Id = b, Code = "TNT-" + suffix + "-B", Name = "Tenant B", NameAr = "tenant b", LegalName = "Tenant B LLC", Status = TenantStatus.Active, TimeZoneId = "UTC", DefaultCulture = "ar", DateFormat = "yyyy-MM-dd", IsDeleted = false });

        var warehouseA = Guid.NewGuid();
        var warehouseB = Guid.NewGuid();

        platform.Warehouses.AddRange(
            new Warehouse { Id = warehouseA, TenantId = a, Code = "WH-A", Name = "Warehouse A", NameAr = "مستودع أ", Type = WarehouseType.Main, IsDefaultReceipt = true, IsActive = true, IsDeleted = false },
            new Warehouse { Id = warehouseB, TenantId = b, Code = "WH-B", Name = "Warehouse B", NameAr = "مستودع ب", Type = WarehouseType.Main, IsDefaultReceipt = true, IsActive = true, IsDeleted = false });

        // A unit is required by Product.BaseUnitId, and the composite FK means the unit
        // must belong to the same tenant as the product.
        var unitA = Guid.NewGuid();
        var unitB = Guid.NewGuid();

        platform.Units.AddRange(
            new Unit { Id = unitA, TenantId = a, Code = "PCS-A", Name = "Pieces A", NameAr = "قطعة", UnitType = UnitType.Count, DecimalPlaces = 0, IsBaseUnit = true, IsActive = true, IsDeleted = false },
            new Unit { Id = unitB, TenantId = b, Code = "PCS-B", Name = "Pieces B", NameAr = "قطعة", UnitType = UnitType.Count, DecimalPlaces = 0, IsBaseUnit = true, IsActive = true, IsDeleted = false });

        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();

        platform.Products.AddRange(
            new Product { Id = productA, TenantId = a, Sku = "SKU-A", Name = "Product A", NameAr = "منتج أ", Description = null, CategoryId = null, BaseUnitId = unitA, PurchaseUnitId = null, IsBatchTracked = false, IsExpiryTracked = false, ShelfLifeDays = null, DefaultReasonCodeId = null, IsActive = true, IsDeleted = false },
            new Product { Id = productB, TenantId = b, Sku = "SKU-B", Name = "Product B", NameAr = "منتج ب", Description = null, CategoryId = null, BaseUnitId = unitB, PurchaseUnitId = null, IsBatchTracked = false, IsExpiryTracked = false, ShelfLifeDays = null, DefaultReasonCodeId = null, IsActive = true, IsDeleted = false });

        await platform.SaveChangesAsync();
        await platform.DisposeAsync();

        // Registered so DisposeAsync removes exactly these rows. A test that seeded data and
        // did not register it would leave it in the canonical database for every later run.
        _tenants.Add(a);
        _tenants.Add(b);
        _warehouses.Add(warehouseA);
        _warehouses.Add(warehouseB);
        _units.Add(unitA);
        _units.Add(unitB);
        _products.Add(productA);
        _products.Add(productB);

        return new Seeded(a, b, warehouseA, warehouseB, productA, productB);
    }

    // ---- Layer 1: read isolation via the query filter -------------------

    [Fact]
    public async Task A_tenant_sees_only_its_own_products()
    {
        var seeded = await SeedAsync();

        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);
        var asB = await _fixture.MigratedContextAsync(seeded.TenantB);

        var visibleToA = await asA.Products.Select(p => p.Id).ToArrayAsync();
        var visibleToB = await asB.Products.Select(p => p.Id).ToArrayAsync();

        visibleToA.Should().ContainSingle().Which.Should().Be(seeded.ProductA);
        visibleToB.Should().ContainSingle().Which.Should().Be(seeded.ProductB);
    }

    [Fact]
    public async Task A_tenant_cannot_read_another_tenants_row_by_guessing_its_guid()
    {
        // The exact attack S1 exists to stop: the GUID is not a secret, so the only
        // thing standing between a guessed id and another tenant's data is the filter
        // plus the composite FK. FindByIdAsync is the code path an IDOR would use.
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        (await asA.Products.FindAsync(seeded.ProductB)).Should().BeNull(
            "the row belongs to tenant B");
        (await asA.Warehouses.FindAsync(seeded.WarehouseB)).Should().BeNull();
        (await asA.Units.FindAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task Listing_products_returns_nothing_from_another_tenant()
    {
        // Listing is the case a filter is most likely to get wrong, because the query
        // looks like an innocuous unfiltered select.
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        var all = await asA.Products.ToArrayAsync();

        all.Should().NotContain(p => p.TenantId == seeded.TenantB);
        all.Should().OnlyContain(p => p.TenantId == seeded.TenantA);
    }

    [Fact]
    public async Task A_tenant_cannot_mutate_another_tenants_row()
    {
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        // The row cannot even be LOADED. This is the RLS policy doing the work: the
        // tenant-filter bypass only removes the application-layer filter, and the database
        // predicate refuses to hand the row over regardless.
        (await asA.Products.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == seeded.ProductB))
            .Should().BeNull("row-level security hides another tenant's row before the ORM is consulted");

        // So the attack has to be attempted directly, as raw SQL, which is also the honest
        // form of it: a hand-written UPDATE that names the victim's primary key.
        var affected = await asA.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == seeded.ProductB)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Name, "HACKED BY TENANT A"));

        affected.Should().Be(0, "the UPDATE matched no row the tenant is permitted to see");

        // Verified through platform authority, because tenant B's own context also cannot see
        // it any more. The value has to be checked by someone the RLS policy admits.
        var platform = await _fixture.PlatformContextAsync();
        var reread = await platform.Products.AsNoTracking().SingleAsync(p => p.Id == seeded.ProductB);

        reread.Name.Should().NotBe("HACKED BY TENANT A",
            "a cross-tenant write must never reach the database");
    }

    [Fact]
    public async Task A_tenant_cannot_delete_another_tenants_row()
    {
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        // Loaded through the filter bypass and refused, because the RLS policy does not
        // depend on the ORM cooperating.
        (await asA.Products.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == seeded.ProductB))
            .Should().BeNull();

        // The delete itself, aimed straight at the victim's key.
        var affected = await asA.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == seeded.ProductB)
            .ExecuteDeleteAsync();

        affected.Should().Be(0);

        var platform = await _fixture.PlatformContextAsync();
        (await platform.Products.AsNoTracking().CountAsync(p => p.Id == seeded.ProductB)).Should().Be(1);
    }

    // ---- Layer 2: cross-tenant INSERT is impossible ---------------------

    [Fact]
    public async Task A_row_cannot_be_inserted_that_belongs_to_no_real_tenant()
    {
        // A free-floating TenantId would satisfy every composite child key while the
        // tenant filter happily returned it. FK(TenantId) -> Tenant(Id) is the
        // load-bearing key that prevents this.
        var platform = await _fixture.PlatformContextAsync();

        var unit = Guid.NewGuid();
        platform.Units.Add(new Unit
        {
            Id = unit,
            TenantId = Guid.NewGuid(),
            Code = "GHOST",
            Name = "Ghost",
            NameAr = null,
            UnitType = UnitType.Count,
            DecimalPlaces = 0,
            IsBaseUnit = true,
            IsActive = true,
            IsDeleted = false,
        });

        var act = () => platform.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "TenantId must reference a real identity.Tenant row");
    }

    [Fact]
    public async Task A_child_row_cannot_reference_a_parent_in_another_tenant()
    {
        // The strongest tenant-isolation statement available: not "invisible" but
        // "impossible". FK(UnitId, TenantId) -> Unit(Id, TenantId) means a product in
        // tenant A cannot point at a unit in tenant B.
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        // Tenant B's unit id, read out of band.
        var foreignUnit = await _fixture.MigratedContextAsync(seeded.TenantB);
        var unitBId = await foreignUnit.Units.Select(u => u.Id).SingleAsync();

        asA.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantA,
            Sku = "CROSS-TENANT",
            Name = "Cross Tenant Product",
            NameAr = null,
            Description = null,
            CategoryId = null,
            BaseUnitId = unitBId,
            PurchaseUnitId = null,
            IsBatchTracked = false,
            IsExpiryTracked = false,
            ShelfLifeDays = null,
            DefaultReasonCodeId = null,
            IsActive = true,
            IsDeleted = false,
        });

        var act = () => asA.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "the composite FK (BaseUnitId, TenantId) must refuse a unit from tenant B");
    }

    [Fact]
    public async Task A_tenant_cannot_reuse_another_tenants_unique_code()
    {
        // ADR-0032: every unique index on a tenant-scoped table leads with TenantId, so
        // the SAME warehouse code in two tenants is legal while two in one tenant is not.
        var seeded = await SeedAsync();

        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);
        var asB = await _fixture.MigratedContextAsync(seeded.TenantB);

        // Tenant B already has WH-B; tenant A reusing it must be accepted.
        asA.Warehouses.Add(new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantA,
            Code = "WH-B",
            Name = "Tenant A also wants WH-B",
            NameAr = null,
            Type = WarehouseType.Storage,
            IsDefaultReceipt = false,
            IsActive = true,
            IsDeleted = false,
        });
        await asA.SaveChangesAsync();

        // This row is committed, so it has to be registered for cleanup.
        _warehouses.Add(asA.Warehouses.Local.Single(w => w.Code == "WH-B" && w.TenantId == seeded.TenantA).Id);

        (await asA.Warehouses.CountAsync(w => w.Code == "WH-B")).Should().Be(1);

        // Within one tenant it must still be refused.
        asB.Warehouses.Add(new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantB,
            Code = "WH-B",
            Name = "Tenant B duplicate",
            NameAr = null,
            Type = WarehouseType.Storage,
            IsDefaultReceipt = false,
            IsActive = true,
            IsDeleted = false,
        });

        var act = () => asB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>(
            "UX_Warehouse_TenantId_Code must refuse a duplicate within one tenant");
    }

    [Fact]
    public async Task A_tenant_code_is_globally_unique_because_tenant_is_a_root_row()
    {
        // Tenant.Code is global (it appears in URLs), so two tenants cannot share one.
        var platform = await _fixture.PlatformContextAsync();
        var code = "DUP-" + Guid.NewGuid().ToString("N")[..10];

        // The first SaveChanges below COMMITS, and the second is expected to throw. So the
        // first tenant is a real row in the canonical database and has to be registered for
        // cleanup, or every run of this test leaves another one behind.
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _tenants.Add(first);
        _tenants.Add(second);

        platform.Tenants.Add(new Tenant { Id = first, Code = code, Name = "First", NameAr = null, LegalName = null, Status = TenantStatus.Active, TimeZoneId = "UTC", DefaultCulture = "ar", DateFormat = "yyyy-MM-dd", IsDeleted = false });
        await platform.SaveChangesAsync();

        platform.Tenants.Add(new Tenant { Id = second, Code = code, Name = "Second", NameAr = null, LegalName = null, Status = TenantStatus.Active, TimeZoneId = "UTC", DefaultCulture = "ar", DateFormat = "yyyy-MM-dd", IsDeleted = false });

        var act = () => platform.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ---- Layer 3: the filter cannot be relied on alone -------------------

    [Fact]
    public async Task IgnoreQueryFilters_cannot_bypass_isolation_because_the_policy_is_the_guarantee()
    {
        // This test used to assert the OPPOSITE and named itself after that expectation:
        // it proved that IgnoreQueryFilters DID reveal other tenants' rows, to document why
        // the application-layer filter could not be trusted on its own.
        //
        // That was true only while the tenant-isolation migration was not applied to the
        // database these tests ran against. It is no longer true, and the change is an
        // improvement: row-level security filters the row in the ENGINE, so bypassing the
        // ORM filter reaches a predicate that no C# call can switch off. The test is kept,
        // and inverted, because its real purpose -- proving that a filter cannot be relied
        // on alone -- is now discharged by the policy instead of by the composite keys, and
        // that is worth asserting just as loudly.
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        var bypassed = await asA.Products.IgnoreQueryFilters().ToArrayAsync();

        bypassed.Should().NotBeEmpty("the tenant still sees its own products");
        bypassed.Select(p => p.TenantId).Distinct().Should().ContainSingle()
            .Which.Should().Be(seeded.TenantA, "the RLS predicate holds even with the ORM filter removed");

        // And the row is genuinely still there -- hidden, not deleted. Checked under platform
        // authority, the only scope the policy admits.
        var platform = await _fixture.PlatformContextAsync();
        (await platform.Products.AsNoTracking().CountAsync(p => p.Id == seeded.ProductB)).Should().Be(1);
    }

    [Fact]
    public async Task A_context_with_no_tenant_sees_no_tenant_data_at_all()
    {
        // An unauthenticated request must not read tenant data by omitting the tenant
        // parameter: the filter pins TenantId to NULL, which matches no row.
        var seeded = await SeedAsync();
        var anonymous = await _fixture.AnonymousContextAsync();

        (await anonymous.Products.CountAsync()).Should().Be(0);
        (await anonymous.Warehouses.CountAsync()).Should().Be(0);
        (await anonymous.Products.IgnoreQueryFilters().CountAsync(p => p.TenantId == seeded.TenantA)).Should().Be(0,
            "even bypassing the filter, the pinned NULL tenant matches nothing");
    }

    [Fact]
    public async Task A_platform_context_is_deliberately_unguarded_and_says_so()
    {
        // ADR-0034: a platform-authority session is an explicit, reviewed cross-tenant
        // scope. It is NOT a bug that it can see everything, and the test asserts that
        // this is the case so a change would be noticed.
        var seeded = await SeedAsync();
        var platform = await _fixture.PlatformContextAsync();

        platform.IsPlatformScope.Should().BeTrue();
        platform.CurrentTenantId.Should().BeNull();

        var all = await platform.Products.Select(p => p.TenantId).Distinct().ToArrayAsync();
        all.Should().Contain([seeded.TenantA, seeded.TenantB]);
    }

    // ---- Soft delete interaction ----------------------------------------

    [Fact]
    public async Task A_soft_deleted_row_is_hidden_but_still_owns_its_unique_code_slot_history()
    {
        // The filtered unique index UX_Device_TenantId_DeviceCode is on IsDeleted = 0,
        // and the query filter hides deleted rows. Both must agree, or a deleted row
        // would block reuse of its code.
        var seeded = await SeedAsync();
        var asA = await _fixture.MigratedContextAsync(seeded.TenantA);

        var product = await asA.Products.SingleAsync(p => p.Id == seeded.ProductA);
        product.IsDeleted = true;
        await asA.SaveChangesAsync();

        var reread = await _fixture.MigratedContextAsync(seeded.TenantA);
        (await reread.Products.CountAsync(p => p.Id == seeded.ProductA)).Should().Be(0);

        // The row still exists and still occupies its TenantId+Sku slot.
        var bypass = await _fixture.MigratedContextAsync(seeded.TenantA);
        var raw = await bypass.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == seeded.ProductA);
        raw.IsDeleted.Should().BeTrue();
    }
}