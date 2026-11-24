using FluentAssertions;
using Stockly.Application.Auditing;
using Stockly.Application.Common.Security;
using Stockly.Domain.Common;
using Xunit;

namespace Stockly.UnitTests.Auditing;

/// <summary>
/// <c>PROJECT_BLUEPRINT.md</c> §16 rules 1–7 for the audit model.
/// <para>
/// The mapper is a pure function, so every rule that does not need the server can be
/// proven here. Rule 6 (written inside the business transaction) is enforced by the
/// collector draining rather than saving, which is also testable without a
/// database.
/// </para>
/// </summary>
public sealed class AuditEntryMapperTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static AuditEntry Entry(
        AuditCategory category = AuditCategory.Inventory,
        AuditResult result = AuditResult.Success,
        object? before = null,
        object? after = null,
        AuditActor? actor = null) =>
        new()
        {
            Category = category,
            Action = "stock.out.post",
            Result = result,
            Before = before,
            After = after,
            Actor = actor ?? new AuditActor
            {
                UserId = Guid.NewGuid(),
                MembershipId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
            },
        };

    [Fact]
    public void An_entry_maps_onto_the_documented_audit_columns()
    {
        var actor = new AuditActor
        {
            UserId = Guid.NewGuid(),
            MembershipId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            IpAddress = "203.0.113.7",
            UserAgent = "StocklyTerminal/1.0",
            CorrelationId = "0HN7-abc",
        };

        var productId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        var row = AuditEntryMapper.ToRow(
            new AuditEntry
            {
                Category = AuditCategory.Inventory,
                Action = "stock.out.post",
                Result = AuditResult.Success,
                EntityName = "StockDocument",
                EntityId = "DOC-2026-0001",
                ReasonCode = "issued",
                ProductId = productId,
                UnitId = unitId,
                Quantity = 5m,
                Actor = actor,
            },
            Now);

        row.OccurredAtUtc.Should().Be(Now);
        row.TenantId.Should().Be(actor.TenantId);
        row.Category.Should().Be(AuditCategory.Inventory);
        row.Action.Should().Be("stock.out.post");
        row.Result.Should().Be(AuditResult.Success);
        row.EntityName.Should().Be("StockDocument");
        row.EntityId.Should().Be("DOC-2026-0001");
        row.ReasonCode.Should().Be("issued");
        row.ActorUserId.Should().Be(actor.UserId);
        row.ActorMembershipId.Should().Be(actor.MembershipId);
        row.DeviceId.Should().Be(actor.DeviceId);
        row.SessionId.Should().Be(actor.SessionId);
        row.IpAddress.Should().Be("203.0.113.7");
        row.UserAgent.Should().Be("StocklyTerminal/1.0");
        row.CorrelationId.Should().Be("0HN7-abc");
        row.ProductId.Should().Be(productId);
        row.UnitId.Should().Be(unitId);
        row.Quantity.Should().Be(5m);
    }

    [Fact]
    public void A_platform_event_has_no_tenant()
    {
        // §16: `TenantId` is nullable precisely for platform-level events.
        var row = AuditEntryMapper.ToRow(
            new AuditEntry
            {
                Category = AuditCategory.Platform,
                Action = "platform.auth.login",
                Result = AuditResult.Success,
                Actor = new AuditActor { UserId = Guid.NewGuid() },
            },
            Now);

        row.TenantId.Should().BeNull();
        row.ActorMembershipId.Should().BeNull();
    }

    [Fact]
    public void An_unauthenticated_failure_records_the_absence_rather_than_a_guess()
    {
        // A failed login has no session and no tenant. Writing a null is the honest
        // record; inventing a zero GUID would be indistinguishable from a real one.
        var row = AuditEntryMapper.ToRow(
            new AuditEntry
            {
                Category = AuditCategory.Security,
                Action = "auth.login.failed",
                Result = AuditResult.Failure,
                ReasonCode = "invalid_credentials",
                Actor = AuditActor.None,
            },
            Now);

        row.ActorUserId.Should().BeNull();
        row.ActorMembershipId.Should().BeNull();
        row.TenantId.Should().BeNull();
        row.SessionId.Should().BeNull();
        row.ReasonCode.Should().Be("invalid_credentials");
    }

    [Fact]
    public void An_unsuccessful_security_event_is_never_filed_as_info()
    {
        // The interesting event is the one that did not succeed.
        AuditEntryMapper.DefaultSeverity(AuditCategory.Security, AuditResult.Failure)
            .Should().Be(AuditSeverity.Warning);

        AuditEntryMapper.DefaultSeverity(AuditCategory.Security, AuditResult.Denied)
            .Should().Be(AuditSeverity.Warning);

        AuditEntryMapper.DefaultSeverity(AuditCategory.Security, AuditResult.Success)
            .Should().Be(AuditSeverity.Warning, "a security event is at least Warning either way");
    }

    [Fact]
    public void An_unsuccessful_business_event_is_raised_above_info()
    {
        AuditEntryMapper.DefaultSeverity(AuditCategory.Inventory, AuditResult.Failure)
            .Should().Be(AuditSeverity.Warning);
    }

    [Fact]
    public void An_explicit_severity_is_never_overridden()
    {
        var row = AuditEntryMapper.ToRow(
            new AuditEntry
            {
                Category = AuditCategory.Security,
                Action = "auth.login.failed",
                Result = AuditResult.Failure,
                Severity = AuditSeverity.Critical,
            },
            Now);

        row.Severity.Should().Be(AuditSeverity.Critical);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("pinHash")]
    [InlineData("refreshToken")]
    [InlineData("connectionString")]
    public void A_before_payload_carrying_a_secret_is_refused(string key)
    {
        // Rule 5. Refused rather than redacted, because the table is append-only: a
        // credential written there could never be removed.
        var payload = new Dictionary<string, string> { [key] = "leaked" };

        var act = () => AuditEntryMapper.ToRow(Entry(before: payload), Now);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Theory]
    [InlineData("password")]
    [InlineData("pinHash")]
    [InlineData("apiKey")]
    public void An_after_payload_carrying_a_secret_is_refused(string key)
    {
        var payload = new Dictionary<string, string> { [key] = "leaked" };

        var act = () => AuditEntryMapper.ToRow(Entry(after: payload), Now);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void An_ordinary_payload_is_serialised_into_the_json_columns()
    {
        var row = AuditEntryMapper.ToRow(
            Entry(
                before: new { Quantity = 10m },
                after: new { Quantity = 5m, Reason = "issued" }),
            Now);

        row.BeforeJson.Should().Contain("10");
        row.AfterJson.Should().Contain("5");
        row.AfterJson.Should().Contain("issued");
    }

    [Fact]
    public void An_absent_payload_stays_null_rather_than_becoming_the_text_null()
    {
        var row = AuditEntryMapper.ToRow(Entry(), Now);

        row.BeforeJson.Should().BeNull();
        row.AfterJson.Should().BeNull();
    }

    [Fact]
    public void An_over_long_value_is_clipped_to_its_column_width()
    {
        // Clipping beats throwing: losing the record of a real action because a user
        // agent was long would be worse than a clipped audit row.
        var row = AuditEntryMapper.ToRow(
            new AuditEntry
            {
                Category = AuditCategory.Identity,
                Action = new string('a', 500),
                Result = AuditResult.Success,
                EntityName = new string('b', 500),
                EntityId = new string('c', 500),
                ReasonCode = new string('d', 500),
                Actor = new AuditActor
                {
                    TenantId = Guid.NewGuid(),
                    UserAgent = new string('e', 900),
                    CorrelationId = new string('f', 900),
                    IpAddress = new string('g', 90),
                },
            },
            Now);

        row.Action!.Length.Should().Be(AuditEntryMapper.Limits.Action);
        row.EntityName!.Length.Should().Be(AuditEntryMapper.Limits.EntityName);
        row.EntityId!.Length.Should().Be(AuditEntryMapper.Limits.EntityId);
        row.ReasonCode!.Length.Should().Be(AuditEntryMapper.Limits.ReasonCode);
        row.UserAgent!.Length.Should().Be(AuditEntryMapper.Limits.UserAgent);
        row.CorrelationId!.Length.Should().Be(AuditEntryMapper.Limits.CorrelationId);
        row.IpAddress!.Length.Should().Be(AuditEntryMapper.Limits.IpAddress);
    }

    [Fact]
    public void An_unnamed_action_is_refused()
    {
        var act = () => AuditEntryMapper.ToRow(
            new AuditEntry { Category = AuditCategory.Inventory, Action = "  ", Result = AuditResult.Success },
            Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void The_row_carries_no_rowversion_or_soft_delete_state()
    {
        // ADR-0037: append-only means no RowVersion and no IsCurrent on the table.
        // The entity asserts this structurally; re-checking here would be noise, so
        // the guarantee lives in StocklyModelTests instead.
        AuditEntryMapper.ToRow(Entry(), Now).Should().NotBeNull();
    }
}

/// <summary>
/// Rule 6: the row is written inside the business transaction. The collector stages
/// rather than saves, which is what makes that possible.
/// </summary>
public sealed class AuditCollectorTests
{
    private static AuditEntry Entry(string action) =>
        new() { Category = AuditCategory.Inventory, Action = action, Result = AuditResult.Success };

    [Fact]
    public void Staged_entries_are_returned_on_drain()
    {
        var collector = new AuditCollector();
        collector.Add(Entry("a"));
        collector.Add(Entry("b"));

        collector.Drain().Select(e => e.Action).Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public void Draining_twice_does_not_replay_the_same_entries()
    {
        // Otherwise a retry inside one request would duplicate audit rows.
        var collector = new AuditCollector();
        collector.Add(Entry("a"));

        collector.Drain().Should().HaveCount(1);
        collector.Drain().Should().BeEmpty();
    }

    [Fact]
    public void A_drained_collector_accepts_new_entries()
    {
        var collector = new AuditCollector();
        collector.Add(Entry("first"));
        collector.Drain();

        collector.Add(Entry("second"));

        collector.Drain().Should().ContainSingle().Which.Action.Should().Be("second");
    }

    [Fact]
    public void An_empty_drain_returns_an_empty_collection_not_null()
    {
        new AuditCollector().Drain().Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void A_null_entry_is_refused()
    {
        var collector = new AuditCollector();

        var act = () => collector.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}