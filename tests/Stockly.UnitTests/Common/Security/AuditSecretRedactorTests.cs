using System.Text.Json;
using FluentAssertions;
using Stockly.Application.Common.Security;
using Xunit;

namespace Stockly.UnitTests.Common.Security;

/// <summary>
/// AGENTS.md §4 and SECURITY_ARCHITECTURE.md §13: passwords, PINs, PIN hashes,
/// JWTs, refresh tokens, encryption keys and connection strings must never be
/// logged, returned or persisted. <c>AuditSecretRedactor</c> is the control that
/// makes that true for audit payloads.
/// </summary>
public sealed class AuditSecretRedactorTests
{
    [Theory]
    [InlineData("Password")]
    [InlineData("password")]
    [InlineData("password_hash")]
    [InlineData("PasswordHash")]
    [InlineData("PASSWORDHASH")]
    [InlineData("NewPassword")]
    [InlineData("Old_Password")]
    [InlineData("pwd")]
    [InlineData("passphrase")]
    [InlineData("Pin")]
    [InlineData("pin")]
    [InlineData("pinHash")]
    [InlineData("pin_hash")]
    [InlineData("PinCode")]
    [InlineData("PINHASH")]
    [InlineData("TerminalPin")]
    [InlineData("secret")]
    [InlineData("Secret")]
    [InlineData("clientSecret")]
    [InlineData("salt")]
    [InlineData("pepper")]
    [InlineData("jwt")]
    [InlineData("RefreshToken")]
    [InlineData("refresh_token")]
    [InlineData("refreshTokenHash")]
    [InlineData("AccessToken")]
    [InlineData("idToken")]
    [InlineData("tokenHash")]
    [InlineData("connectionString")]
    [InlineData("Connection_String")]
    [InlineData("privateKey")]
    [InlineData("encryptionKey")]
    [InlineData("signingKey")]
    [InlineData("apiKey")]
    [InlineData("credentials")]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("SecretSauce")] // the `secret` token is forbidden even inside a larger word
    [InlineData("CredentialType")] // `credential` is forbidden; the suffix does not save it
    public void A_secret_named_property_is_refused(string name)
    {
        AuditSecretRedactor.IsForbiddenName(name).Should().BeTrue(name);
    }

    [Theory]
    [InlineData("shipping")]
    [InlineData("spinning")]
    [InlineData("ShippingAddress")]
    [InlineData("Pinned")]
    [InlineData("caption")]
    [InlineData("Description")]
    [InlineData("Quantity")]
    [InlineData("ProductName")]
    [InlineData("WarehouseCode")]
    [InlineData("DocumentNumber")]
    [InlineData("Status")]
    [InlineData("TotalCost")]
    [InlineData("IsActive")]
    [InlineData("saltiness")]
    [InlineData("Peppercorn")]
    [InlineData("TokenCount")]
    public void An_ordinary_property_is_not_a_false_positive(string name)
    {
        // The whole point of matching whole tokens rather than substrings: a
        // substring rule on "pin" would reject `shipping`, `spinning` and
        // `caption` and make the control useless through alert fatigue.
        AuditSecretRedactor.IsForbiddenName(name).Should().BeFalse(name);
    }

    [Fact]
    public void A_jwt_shaped_value_is_refused_under_an_innocuous_name()
    {
        // Naming style and placement must not be the only defence: a token pasted
        // into a field called `note` is still a token.
        var payload = new Dictionary<string, object?>
        {
            ["note"] = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.abcdefghijklmnop",
        };

        var act = () => AuditSecretRedactor.EnsureNoSecrets(payload);

        act.Should().Throw<AuditPayloadSecretException>()
            .WithMessage("*JWT*");
    }

    [Fact]
    public void A_connection_string_fragment_is_refused_under_an_innocuous_name()
    {
        var payload = new Dictionary<string, object?>
        {
            ["notes"] = "Server=sql-prod-01;Database=Stockly;User Id=sa;Password=hunter2;",
        };

        var act = () => AuditSecretRedactor.EnsureNoSecrets(payload);

        act.Should().Throw<AuditPayloadSecretException>()
            .WithMessage("*connection-string*");
    }

    [Fact]
    public void A_secret_property_is_refused_in_an_anonymous_object()
    {
        var act = () => AuditSecretRedactor.EnsureNoSecrets(new { Name = "Rice", Password = "hunter2" });

        act.Should().Throw<AuditPayloadSecretException>()
            .WithMessage("*Password*");
    }

    [Fact]
    public void A_secret_property_is_refused_inside_a_nested_object_graph()
    {
        var payload = new
        {
            EntityName = "Membership",
            After = new
            {
                RoleId = Guid.NewGuid(),
                Credential = new { PinHash = "abc" },
            },
        };

        var act = () => AuditSecretRedactor.EnsureNoSecrets(payload);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void A_secret_property_is_refused_inside_a_collection()
    {
        var payload = new
        {
            Lines = new[]
            {
                new LineChange { Sku = "RICE-25KG", Quantity = 3m, PinHash = null },
                new LineChange { Sku = "OIL-1L", Quantity = 1m, PinHash = "leaked" },
            },
        };

        var act = () => AuditSecretRedactor.EnsureNoSecrets(payload);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void A_secret_property_is_refused_inside_serialised_json()
    {
        // Audit payloads routinely arrive as JSON rather than as object graphs.
        using var document = JsonDocument.Parse("""
            { "sku": "RICE-25KG", "before": { "pinHash": "leaked" } }
            """);

        var act = () => AuditSecretRedactor.EnsureNoSecrets(document);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void An_ordinary_audit_payload_passes()
    {
        var payload = new AuditPayload
        {
            EntityName = "Product",
            EntityId = Guid.NewGuid().ToString(),
            BeforeJson = null,
            After = new ProductChange { NameAr = "رز", NameEn = "Rice 25kg", IsActive = true },
            Quantity = 12.5m,
            Codes = ["RICE-25KG", "OIL-1L"],
        };

        var act = () => AuditSecretRedactor.EnsureNoSecrets(payload);

        act.Should().NotThrow();
    }

    [Fact]
    public void A_null_payload_passes()
    {
        var act = () => AuditSecretRedactor.EnsureNoSecrets(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deep_nesting_is_bounded_rather_than_walking_without_limit()
    {
        // An unbounded traversal in the audit path is a denial-of-service vector in
        // the one path that must always be able to record, so depth is capped.
        object node = new { Value = 1 };
        for (var i = 0; i < 200; i++)
        {
            node = new { Child = node };
        }

        var act = () => AuditSecretRedactor.EnsureNoSecrets(node);

        act.Should().NotThrow();
    }

    [Fact]
    public void A_throwing_property_getter_does_not_break_auditing()
    {
        // The audit writer must be able to record even when one field is hostile.
        var act = () => AuditSecretRedactor.EnsureNoSecrets(new Exploding());

        act.Should().NotThrow();
    }

    [Fact]
    public void The_refusal_is_a_developer_defect_not_a_business_error()
    {
        // It must not be mappable to a documented business code: a client error
        // response would tell a caller that a secret field existed.
        var ex = new AuditPayloadSecretException("x");

        ex.Should().NotBeAssignableTo<Stockly.Application.Common.Errors.BusinessException>();
    }

    private sealed class Exploding
    {
        public string Name => "ok";

        public string Boom => throw new InvalidOperationException("getter failed");
    }

    private sealed class LineChange
    {
        public string? Sku { get; init; }

        public decimal Quantity { get; init; }

        public string? PinHash { get; init; }
    }

    private sealed class ProductChange
    {
        public string? NameAr { get; init; }

        public string? NameEn { get; init; }

        public bool IsActive { get; init; }
    }

    private sealed class AuditPayload
    {
        public string? EntityName { get; init; }

        public string? EntityId { get; init; }

        public string? BeforeJson { get; init; }

        public ProductChange? After { get; init; }

        public decimal Quantity { get; init; }

        public string[] Codes { get; init; } = [];
    }
}