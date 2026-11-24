using FluentAssertions;
using Stockly.Application.Security;
using Stockly.Domain.Common;
using Xunit;

namespace Stockly.UnitTests.Security;

/// <summary>
/// The ADR-0034 scope-isolation rule.
/// <para>
/// A pure function over (route, scope), so the invariant is provable with no
/// database, no token issuer and no HTTP pipeline.
/// </para>
/// </summary>
public sealed class RouteScopeResolverTests
{
    [Theory]
    [InlineData("/api/v1/inventory/balances", RouteScope.Tenant)]
    [InlineData("/api/v1/orders/9f0e.../lines", RouteScope.Tenant)]
    [InlineData("/api/v1/terminal/sessions/ABC/identities", RouteScope.Tenant)]
    [InlineData("/api/v1/platform/auth/login", RouteScope.Platform)]
    [InlineData("/api/v1/platform/tenants", RouteScope.Platform)]
    [InlineData("/api/v1/platform/tenants/abc/warehouses", RouteScope.Platform)]
    [InlineData("/health", RouteScope.Anonymous)]
    [InlineData("/api/v1", RouteScope.Tenant)]
    [InlineData("/api/v2/platform/tenants", RouteScope.Anonymous)]
    [InlineData("/api/v1/platformx/tenants", RouteScope.Tenant)]
    [InlineData("/api/v1/platform", RouteScope.Tenant)]
    [InlineData("", RouteScope.Anonymous)]
    public void Route_is_classified_from_the_path_alone(string path, RouteScope expected)
    {
        RouteScopeResolver.Resolve(path).Should().Be(expected);
    }

    private static RequestScope TenantScope() => new()
    {
        UserId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        Scope = SessionScope.Tenant,
        TenantId = Guid.NewGuid(),
        MembershipId = Guid.NewGuid(),
    };

    private static RequestScope PlatformScope() => new()
    {
        UserId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        Scope = SessionScope.Platform,
    };

    [Fact]
    public void A_tenant_token_reaches_tenant_routes()
    {
        var scope = TenantScope();

        RouteScopeResolver.Allows(scope, RouteScope.Tenant).Should().BeTrue();
    }

    [Fact]
    public void A_platform_token_is_rejected_by_every_tenant_route()
    {
        // ADR-0034 / S25: authority does not accumulate across scopes.
        RouteScopeResolver.Allows(PlatformScope(), RouteScope.Tenant).Should().BeFalse();
    }

    [Fact]
    public void A_platform_token_is_still_rejected_when_the_user_also_holds_a_tenant_membership()
    {
        // The single most important line of S25: the same *user* holding an active
        // tenant membership does not make a platform token acceptable on a tenant
        // route. Authority does not transfer.
        var sameUserId = Guid.NewGuid();

        var platform = new RequestScope
        {
            UserId = sameUserId,
            SessionId = Guid.NewGuid(),
            Scope = SessionScope.Platform,
        };

        var tenant = new RequestScope
        {
            UserId = sameUserId,
            SessionId = Guid.NewGuid(),
            Scope = SessionScope.Tenant,
            TenantId = Guid.NewGuid(),
            MembershipId = Guid.NewGuid(),
        };

        RouteScopeResolver.Allows(platform, RouteScope.Tenant).Should().BeFalse();
        RouteScopeResolver.Allows(tenant, RouteScope.Platform).Should().BeFalse();
    }

    [Fact]
    public void A_tenant_token_is_rejected_inside_platform_routes()
    {
        RouteScopeResolver.Allows(TenantScope(), RouteScope.Platform).Should().BeFalse();
    }

    [Fact]
    public void A_platform_token_reaches_platform_routes()
    {
        RouteScopeResolver.Allows(PlatformScope(), RouteScope.Platform).Should().BeTrue();
    }

    [Fact]
    public void An_authenticated_scope_never_satisfies_an_anonymous_route()
    {
        // An anonymous route (health, docs) is handled by its own pipeline; the
        // scope decision must not accidentally permit a scoped token to skip it.
        RouteScopeResolver.Allows(TenantScope(), RouteScope.Anonymous).Should().BeFalse();
        RouteScopeResolver.Allows(PlatformScope(), RouteScope.Anonymous).Should().BeFalse();
    }

    [Fact]
    public void A_platform_scope_carries_no_tenant_or_membership()
    {
        var scope = PlatformScope();

        scope.IsPlatform.Should().BeTrue();
        scope.IsTenant.Should().BeFalse();
        scope.TenantId.Should().BeNull();
        scope.MembershipId.Should().BeNull();
    }

    [Fact]
    public void RequireTenantId_throws_for_a_platform_scope()
    {
        // The warehouse chain has no meaning without a tenant, so the accessor
        // refuses rather than returning a value a caller might forget to check.
        var act = () => PlatformScope().RequireTenantId();

        act.Should().Throw<Stockly.Application.Common.Errors.BusinessException>();
    }

    [Fact]
    public void RequireTenantId_returns_the_tenant_for_a_tenant_scope()
    {
        var scope = TenantScope();

        scope.RequireTenantId().Should().Be(scope.TenantId!.Value);
    }

    [Fact]
    public void A_scope_cannot_be_mutated_after_construction()
    {
        // It is a sealed record with init-only members, so no later code can
        // re-point an in-flight request at another tenant.
        var scope = TenantScope();
        var before = scope.TenantId;

        scope = scope with { UserId = Guid.NewGuid() };

        scope.TenantId.Should().Be(before);
    }
}