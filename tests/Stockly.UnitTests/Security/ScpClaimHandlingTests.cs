using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Stockly.Api.Tenancy;
using Stockly.Application.Security;
using Stockly.Domain.Common;
using Xunit;

namespace Stockly.UnitTests.Security;

/// <summary>
/// Regression suite for the <c>scp</c> handling defects found on 2026-10-01.
/// <para>
/// Three distinct failure modes are covered, and they are kept as separate tests
/// because a fix for one does not fix the others:
/// </para>
/// <list type="number">
/// <item><strong>Casing.</strong> The claim value was compared against
/// <c>SessionScope.Platform.ToString()</c>, which is <c>"Platform"</c>, while the
/// documented wire value is <c>"platform"</c>. Every genuine platform token was
/// classified as a <em>tenant</em> token.</item>
/// <item><strong>Fail-closed.</strong> <c>IsPlatformScope</c> was a two-state
/// answer, so an unreadable scope collapsed to <c>false</c> — meaning "tenant" —
/// and the <c>tid</c> claim was then honoured. <c>TESTING_STRATEGY.md</c> S25
/// requires such a token to be rejected.</item>
/// <item><strong>Numeric enum text.</strong> <c>Enum.TryParse</c> accepts <c>"2"</c>
/// as <c>SessionScope.Platform</c>. Tokens are never issued with a numeric scope,
/// so honouring one is an escalation.</item>
/// </list>
/// <para>
/// Each of these is asserted against <em>both</em> the parser and the binder that
/// consumes it, because the original defect was in the binder and a parser-only fix
/// would have left it live.
/// </para>
/// </summary>
public sealed class ScpClaimHandlingTests
{
    private static HttpTenantContext TenantContext(params Claim[] claims)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return new HttpTenantContext(new HttpContextAccessor { HttpContext = http });
    }

    private static Claim Scp(string value) => new("scp", value);

    // ---- 1. Casing -----------------------------------------------------

    [Theory]
    [InlineData("platform", true)]
    [InlineData("Platform", true)]
    [InlineData("PLATFORM", true)]
    [InlineData("tenant", false)]
    [InlineData("Tenant", false)]
    [InlineData("TENANT", false)]
    public void The_binder_classifies_the_documented_scope_values(string claim, bool expectedPlatform)
    {
        TenantContext(Scp(claim)).IsPlatformScope.Should().Be(expectedPlatform);
    }

    [Fact]
    public void A_lowercase_platform_token_is_not_treated_as_a_tenant_token()
    {
        // The exact regression. Before the fix this returned false.
        TenantContext(Scp("platform")).IsPlatformScope.Should().BeTrue();
    }

    [Fact]
    public void A_platform_token_resolves_no_tenant_even_with_a_tenant_claim_present()
    {
        // Consequence of the casing bug: misclassified as tenant, it read `tid` and
        // produced a tenant-bound context for a platform operator.
        var context = TenantContext(
            Scp("platform"),
            new Claim("tid", Guid.NewGuid().ToString()),
            new Claim("mid", Guid.NewGuid().ToString()));

        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
    }

    // ---- 2. Fail-closed -------------------------------------------------

    [Theory]
    [InlineData("2")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("999999")]
    [InlineData("1.0")]
    public void A_numeric_scope_is_refused_by_the_parser(string claim)
    {
        SessionScopeClaim.TryParse(claim, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("2")]
    [InlineData("1")]
    [InlineData("0")]
    public void A_numeric_scope_never_grants_platform_authority_to_the_binder(string claim)
    {
        // `Enum.TryParse` would return True with Platform for "2".
        var context = TenantContext(
            Scp(claim),
            new Claim("tid", Guid.NewGuid().ToString()),
            new Claim("mid", Guid.NewGuid().ToString()));

        context.IsPlatformScope.Should().BeFalse();
        context.TenantId.Should().BeNull("an unreadable scope must not resolve a tenant");
        context.MembershipId.Should().BeNull();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("root")]
    [InlineData("superuser")]
    [InlineData("PlatformAdmin")]
    [InlineData("platform-admin")]
    [InlineData("tenant platform")]
    [InlineData("platform,tenant")]
    [InlineData("scope:platform")]
    [InlineData("\"platform\"")]
    public void An_unrecognised_scope_yields_no_tenant_at_all(string claim)
    {
        // The distinction that matters: not merely "not platform", but "no tenant",
        // so an unparseable token cannot be used to read tenant data.
        var context = TenantContext(
            Scp(claim),
            new Claim("tid", Guid.NewGuid().ToString()),
            new Claim("mid", Guid.NewGuid().ToString()));

        context.IsPlatformScope.Should().BeFalse();
        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
    }

    [Fact]
    public void A_missing_scope_claim_yields_no_tenant_even_with_a_tenant_claim_present()
    {
        var context = TenantContext(new Claim("tid", Guid.NewGuid().ToString()));

        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
        context.IsPlatformScope.Should().BeFalse();
    }

    [Fact]
    public void An_empty_scope_claim_yields_no_tenant()
    {
        var context = TenantContext(Scp(string.Empty), new Claim("tid", Guid.NewGuid().ToString()));

        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void A_whitespace_scope_claim_yields_no_tenant()
    {
        var context = TenantContext(Scp("   "), new Claim("tid", Guid.NewGuid().ToString()));

        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void A_valid_tenant_scope_still_resolves_its_tenant()
    {
        // The counter-test: fail-closed must not be implemented by refusing
        // everything, which would pass every rejection test while breaking the
        // product.
        var tenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        var context = TenantContext(
            Scp("tenant"),
            new Claim("tid", tenantId.ToString()),
            new Claim("mid", membershipId.ToString()));

        context.TenantId.Should().Be(tenantId);
        context.MembershipId.Should().Be(membershipId);
        context.IsPlatformScope.Should().BeFalse();
    }

    // ---- Header-based escalation attempts --------------------------------

    [Fact]
    public void A_scope_header_cannot_promote_a_tenant_token_to_platform()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["scp"] = "platform";
        http.Request.Headers["X-Scope"] = "platform";
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [Scp("tenant"), new Claim("tid", Guid.NewGuid().ToString())], "test"));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.IsPlatformScope.Should().BeFalse();
    }

    [Fact]
    public void A_scope_header_cannot_give_a_token_whose_claim_is_invalid_a_tenant()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["scp"] = "tenant";
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [Scp("garbage"), new Claim("tid", Guid.NewGuid().ToString())], "test"));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void An_anonymous_principal_with_a_tenant_claim_resolves_nothing()
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("tid", Guid.NewGuid().ToString()), Scp("tenant")]));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.TenantId.Should().BeNull("an unauthenticated principal carries no authority");
    }

    // ---- Round-trip ------------------------------------------------------

    [Theory]
    [InlineData(SessionScope.Tenant)]
    [InlineData(SessionScope.Platform)]
    public void A_scope_survives_serialisation_and_reparsing(SessionScope scope)
    {
        var wire = SessionScopeClaim.ToClaimValue(scope);

        wire.Should().BeOneOf(SessionScopeClaim.Tenant, SessionScopeClaim.Platform);

        SessionScopeClaim.TryParse(wire, out var reparsed).Should().BeTrue();
        reparsed.Should().Be(scope);
    }

    [Fact]
    public void The_wire_values_are_the_lowercase_documented_ones()
    {
        // A change here would break every token issued by Phase 2, so it is pinned.
        SessionScopeClaim.ToClaimValue(SessionScope.Tenant).Should().Be("tenant");
        SessionScopeClaim.ToClaimValue(SessionScope.Platform).Should().Be("platform");
    }

    [Fact]
    public void The_enum_member_names_are_never_the_emitted_wire_values()
    {
        // Guards against a future change emitting `SessionScope.Tenant.ToString()`.
        SessionScope.Tenant.ToString().Should().NotBe(SessionScopeClaim.ToClaimValue(SessionScope.Tenant));
        SessionScope.Platform.ToString().Should().NotBe(SessionScopeClaim.ToClaimValue(SessionScope.Platform));
    }

    [Fact]
    public void Parsing_does_not_mutate_a_shared_default_between_calls()
    {
        // A static parser that cached its out-parameter would leak scope across
        // requests, which in a server is a cross-tenant leak.
        SessionScopeClaim.TryParse("platform", out var first);
        SessionScopeClaim.TryParse("nonsense", out var second);
        SessionScopeClaim.TryParse("tenant", out var third);

        first.Should().Be(SessionScope.Platform);
        second.Should().Be(SessionScope.Tenant);
        third.Should().Be(SessionScope.Tenant);
    }
}