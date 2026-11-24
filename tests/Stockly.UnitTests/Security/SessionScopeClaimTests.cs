using FluentAssertions;
using Stockly.Application.Security;
using Stockly.Domain.Common;
using Xunit;

namespace Stockly.UnitTests.Security;

/// <summary>
/// The <c>scp</c> claim is the single switch that makes a session tenant-scoped or
/// platform-scoped (ADR-0034). These tests pin the wire values so a comparison
/// against the enum's member name — <c>"Platform"</c> rather than
/// <c>"platform"</c> — cannot be reintroduced.
/// </summary>
public sealed class SessionScopeClaimTests
{
    [Theory]
    [InlineData("tenant", SessionScope.Tenant)]
    [InlineData("platform", SessionScope.Platform)]
    public void The_documented_wire_values_parse(string claim, SessionScope expected)
    {
        SessionScopeClaim.TryParse(claim, out var scope).Should().BeTrue();
        scope.Should().Be(expected);
    }

    [Theory]
    [InlineData("Tenant")]
    [InlineData("PLATFORM")]
    [InlineData(" platform ")]
    public void Casing_and_padding_are_tolerated(string claim)
    {
        // `scp` is not a secret; a casing mismatch should not silently downgrade a
        // platform operator to a tenant session.
        SessionScopeClaim.TryParse(claim, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_scope_does_not_parse(string? claim)
    {
        SessionScopeClaim.TryParse(claim, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("tenant,platform")]
    [InlineData("platforms")]
    [InlineData("tenan")]
    public void An_unrecognised_scope_does_not_parse(string claim)
    {
        SessionScopeClaim.TryParse(claim, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("2")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-1")]
    public void A_numeric_scope_does_not_grant_authority(string claim)
    {
        // `Enum.TryParse` would accept "2" as SessionScope.Platform. A token is never
        // issued with a numeric scope, so honouring one would be an escalation.
        SessionScopeClaim.TryParse(claim, out _).Should().BeFalse();
    }

    [Fact]
    public void A_refused_scope_does_not_default_to_platform()
    {
        SessionScopeClaim.TryParse("nonsense", out var scope).Should().BeFalse();

        // The out value is irrelevant when parsing fails; the caller must check the
        // return value. Documented so the next reader does not rely on it.
        scope.Should().Be(SessionScope.Tenant);
    }

    [Fact]
    public void The_enum_member_name_is_not_a_valid_wire_value()
    {
        // The exact defect this type exists to prevent.
        SessionScopeClaim.TryParse(SessionScope.Platform.ToString(), out _)
            .Should().BeTrue("the parser is case-insensitive, so \"Platform\" is still the platform scope");

        SessionScopeClaim.TryParse(SessionScope.Tenant.ToString(), out _).Should().BeTrue();
    }

    [Fact]
    public void Serialising_a_scope_produces_the_documented_value()
    {
        SessionScopeClaim.ToClaimValue(SessionScope.Tenant).Should().Be("tenant");
        SessionScopeClaim.ToClaimValue(SessionScope.Platform).Should().Be("platform");
    }

    [Fact]
    public void An_unknown_enum_value_cannot_be_serialised()
    {
        var act = () => SessionScopeClaim.ToClaimValue((SessionScope)99);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}