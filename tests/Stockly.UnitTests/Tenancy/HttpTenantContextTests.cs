using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Stockly.Api.Tenancy;
using Xunit;

namespace Stockly.UnitTests.Tenancy;

/// <summary>
/// The rule that tenant scope comes from the authenticated session and never from
/// input (<c>AGENTS.md</c> §4, <c>SECURITY_ARCHITECTURE.md</c> §4.4).
/// <para>
/// <see cref="HttpTenantContext"/> is the only place in the codebase that can bind a
/// tenant to an EF query, so these tests are deliberately adversarial about the two
/// ways it could be made to lie: reading a caller-supplied header, and misreading
/// the <c>scp</c> value.
/// </para>
/// </summary>
public sealed class HttpTenantContextTests
{
    private static HttpTenantContext ContextFor(params Claim[] claims)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        return new HttpTenantContext(new HttpContextAccessor { HttpContext = http });
    }

    private static Claim Scp(string value) => new("scp", value);

    private static Claim Guid_Claim(string type, Guid value) => new(type, value.ToString());

    [Fact]
    public void A_tenant_session_resolves_its_tenant_and_membership()
    {
        var tenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        var context = ContextFor(
            Scp("tenant"),
            Guid_Claim("tid", tenantId),
            Guid_Claim("mid", membershipId));

        context.TenantId.Should().Be(tenantId);
        context.MembershipId.Should().Be(membershipId);
        context.IsPlatformScope.Should().BeFalse();
    }

    [Fact]
    public void A_platform_session_resolves_no_tenant_even_when_tid_is_present()
    {
        // Authority does not accumulate across scopes (ADR-0034). A platform token
        // carrying a tid must not be able to read tenant data.
        var context = ContextFor(
            Scp("platform"),
            Guid_Claim("tid", Guid.NewGuid()),
            Guid_Claim("mid", Guid.NewGuid()));

        context.IsPlatformScope.Should().BeTrue();
        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
    }

    [Fact]
    public void The_platform_scope_matches_the_documented_lowercase_claim()
    {
        // Regression guard: comparing against `SessionScope.Platform.ToString()`
        // ("Platform") silently classified every real platform token as a tenant
        // token, which then produced a null tenant and an empty result set.
        ContextFor(Scp("platform")).IsPlatformScope.Should().BeTrue();
    }

    [Fact]
    public void A_tenant_claim_is_not_read_as_a_platform_scope()
    {
        ContextFor(Scp("tenant")).IsPlatformScope.Should().BeFalse();
    }

    [Fact]
    public void A_missing_scope_claim_yields_no_tenant_rather_than_a_guess()
    {
        var context = ContextFor(Guid_Claim("tid", Guid.NewGuid()));

        // The query filter treats a null tenant as "match nothing", so this fails
        // closed instead of matching every tenant row.
        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
        context.IsPlatformScope.Should().BeFalse();
    }

    [Theory]
    [InlineData("2")]
    [InlineData("PlatformAdmin")]
    [InlineData("")]
    public void An_unrecognised_scope_claim_yields_no_tenant(string claim)
    {
        var context = ContextFor(Scp(claim), Guid_Claim("tid", Guid.NewGuid()));

        context.IsPlatformScope.Should().BeFalse();
        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void A_malformed_tenant_claim_yields_null_rather_than_an_empty_guid()
    {
        var context = ContextFor(Scp("tenant"), new Claim("tid", "not-a-guid"), new Claim("mid", ""));

        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
    }

    [Fact]
    public void A_request_with_no_http_context_yields_no_tenant()
    {
        // A background job or a unit test that forgot to bind a tenant must not
        // accidentally see an unscoped context.
        var context = new HttpTenantContext(new HttpContextAccessor());

        context.TenantId.Should().BeNull();
        context.MembershipId.Should().BeNull();
        context.IsPlatformScope.Should().BeFalse();
    }

    [Fact]
    public void An_unauthenticated_principal_yields_no_tenant()
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tid", Guid.NewGuid().ToString())]));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void A_tenant_header_cannot_set_the_tenant()
    {
        // The whole point: no header, query value, route value or body field may
        // nominate a tenant. Only the authenticated claim can.
        var tenantId = Guid.NewGuid();

        var http = new DefaultHttpContext();
        http.Request.Headers["X-Tenant-Id"] = tenantId.ToString();
        http.Request.QueryString = new QueryString($"?tenantId={tenantId}");
        http.Request.RouteValues["tenantId"] = tenantId.ToString();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([], "test"));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void A_tenant_header_does_not_override_an_authenticated_tenant()
    {
        var authenticated = Guid.NewGuid();
        var spoofed = Guid.NewGuid();

        var http = new DefaultHttpContext();
        http.Request.Headers["X-Tenant-Id"] = spoofed.ToString();
        http.Request.Headers["TenantId"] = spoofed.ToString();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("scp", "tenant"), new Claim("tid", authenticated.ToString())],
            "test"));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.TenantId.Should().Be(authenticated);
    }

    [Fact]
    public void A_spoofed_scope_header_cannot_escalate_to_platform()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["scp"] = "platform";
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("scp", "tenant"), new Claim("tid", Guid.NewGuid().ToString())],
            "test"));

        var context = new HttpTenantContext(new HttpContextAccessor { HttpContext = http });

        context.IsPlatformScope.Should().BeFalse("the claim, not the header, decides the scope");
    }

    [Fact]
    public void A_tenant_scope_without_a_membership_still_resolves_the_tenant()
    {
        // The tenant is what the query filter needs. Whether the membership is
        // authorised for the requested warehouse is a later check that must fail.
        var tenantId = Guid.NewGuid();

        var context = ContextFor(Scp("tenant"), Guid_Claim("tid", tenantId));

        context.TenantId.Should().Be(tenantId);
        context.MembershipId.Should().BeNull();
    }
}