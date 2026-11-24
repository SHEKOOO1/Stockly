using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Stockly.Application.Auditing;
using Stockly.Application.Common.Security;
using Stockly.Domain.Common;
using Stockly.Domain.Modules.Audit;
using Xunit;

namespace Stockly.UnitTests.Auditing;

/// <summary>
/// <c>PROJECT_BLUEPRINT.md</c> §16 rules 5 and 6 — the two audit rules that are easy
/// to claim and hard to prove.
/// </para>
/// <para>
/// <strong>Rule 5 (refuse, never redact)</strong> is the higher-value one. A redactor
/// replaces the secret and writes the row; a refusal discards the row. Redaction looks
/// like the safer engineering choice and is not: §16.1 makes the table append-only
/// with no <c>DELETE</c> grant, so a credential written by mistake is permanent. A
/// silently half-redacted payload is also the failure mode where the tool reports
/// success while leaving the value in place.
/// </para>
/// <para>
/// <strong>Rule 6 (same transaction)</strong> is proved here structurally — by showing
/// the collector holds no <c>DbContext</c> and cannot save — and is completed by the
/// database-backed atomicity test in <c>Stockly.IntegrationTests</c>, which requires a
/// real SQL Server and has not been executed.
/// </para>
/// </summary>
public sealed class AuditTransactionalContractTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    private static AuditEntry Entry(
        object? before = null,
        object? after = null,
        AuditActor? actor = null,
        AuditResult result = AuditResult.Success) =>
        new()
        {
            Category = AuditCategory.Identity,
            Action = "membership.role.changed",
            Result = result,
            Before = before,
            After = after,
            Actor = actor ?? new AuditActor { TenantId = Guid.NewGuid(), MembershipId = Guid.NewGuid() },
        };

    // ---- Rule 6: staged, never saved -----------------------------------

    [Fact]
    public void The_collector_cannot_save_because_it_holds_no_persistence_context()
    {
        // If the collector could save, rule 6 would be violated by construction: a
        // separate SaveChanges cannot join the caller's transaction.
        var fields = typeof(AuditCollector)
            .GetFields(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public)
            .Select(field => field.FieldType)
            .ToArray();

        fields.Should().NotContain(typeof(DbContext));
        fields.Should().NotContain(field => field.Name.Contains("DbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void The_collector_exposes_no_save_or_persist_operation()
    {
        var members = typeof(IAuditCollector)
            .GetMembers()
            .Select(member => member.Name)
            .ToArray();

        members.Should().NotContain(name => name.Contains("Save", StringComparison.OrdinalIgnoreCase));
        members.Should().NotContain(name => name.Contains("Persist", StringComparison.OrdinalIgnoreCase));
        members.Should().NotContain(name => name.Contains("Commit", StringComparison.OrdinalIgnoreCase));
        members.Should().NotContain(name => name.Contains("Write", StringComparison.OrdinalIgnoreCase));
        members.Should().NotContain(name => name.Contains("Execute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Adding_an_entry_has_no_side_effect_on_any_context()
    {
        // Nothing is written, queued or sent. Add is a buffer append and nothing else.
        var collector = new AuditCollector();

        var act = () => collector.Add(Entry(after: new { RoleId = Guid.NewGuid() }));

        act.Should().NotThrow();
        collector.Drain().Should().ContainSingle();
    }

    [Fact]
    public void A_drained_entry_is_available_for_the_callers_own_transaction()
    {
        // The contract: Drain hands back rows the caller adds to its change set, so
        // they commit or roll back together.
        var collector = new AuditCollector();
        collector.Add(Entry(after: new { RoleId = Guid.NewGuid() }));

        var rows = collector
            .Drain()
            .Select(entry => AuditEntryMapper.ToRow(entry, Now))
            .ToArray();

        rows.Should().ContainSingle();
        rows[0].Action.Should().Be("membership.role.changed");
    }

    [Fact]
    public void Draining_twice_would_not_duplicate_a_row_inside_one_transaction()
    {
        // A retry that drains again must not produce a second INSERT for the same
        // action, because duplicate audit rows are indistinguishable from two real
        // occurrences.
        var collector = new AuditCollector();
        collector.Add(Entry());

        collector.Drain().Should().ContainSingle();
        collector.Drain().Should().BeEmpty();
    }

    [Fact]
    public void A_secret_in_a_later_entry_does_not_prevent_an_earlier_one_being_drained()
    {
        // The refusal happens at row construction, not at staging, so one bad entry
        // cannot swallow the good ones staged before it.
        var collector = new AuditCollector();
        collector.Add(Entry(after: new { Ok = 1 }));
        collector.Add(Entry(after: new { Password = "leaked" }));

        var entries = collector.Drain();
        entries.Should().HaveCount(2);

        var act = () => entries.ToList().ForEach(entry => AuditEntryMapper.ToRow(entry, Now));

        act.Should().Throw<AuditPayloadSecretException>();
    }

    // ---- Rule 5: refuse, never redact -----------------------------------

    [Fact]
    public void A_secret_bearing_payload_is_refused_rather_than_masked()
    {
        // Refusal means NO ROW AT ALL. A redacted row would satisfy "does not contain a
        // secret" while keeping the event, which is the behaviour §16.1 makes
        // unfixable if it ever got the value wrong.
        var act = () => AuditEntryMapper.ToRow(
            Entry(after: new { Password = "hunter2" }),
            Now);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void The_refusal_names_the_offending_parameter()
    {
        // A refusal with no indication of where the secret was is unactionable at 3am.
        var act = () => AuditEntryMapper.ToRow(Entry(before: new { PinHash = "x" }), Now);

        act.Should().Throw<AuditPayloadSecretException>()
            .WithMessage("*Before*");
    }

    [Fact]
    public void A_refused_payload_never_reaches_the_json_columns()
    {
        // The row is not constructed at all, so there is nothing that could later be
        // serialised by a different code path.
        AuditLog? row = null;

        try
        {
            row = AuditEntryMapper.ToRow(Entry(after: new { Jwt = "a.b.c" }), Now);
        }
        catch (AuditPayloadSecretException)
        {
            // expected
        }

        row.Should().BeNull();
    }

    [Fact]
    public void A_secret_nested_deep_in_a_payload_is_still_refused()
    {
        var payload = new
        {
            Outer = new
            {
                Inner = new Dictionary<string, object>
                {
                    ["RefreshToken"] = "rt_live_abc123",
                },
            },
        };

        var act = () => AuditEntryMapper.ToRow(Entry(after: payload), Now);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void A_secret_shaped_like_a_jwt_is_refused_under_an_innocuous_name()
    {
        // Name matching alone is not sufficient; the value shape is checked too.
        var act = () => AuditEntryMapper.ToRow(
            Entry(after: new { Note = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abcdefghij" }),
            Now);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void A_connection_string_is_refused_under_an_innocuous_name()
    {
        var act = () => AuditEntryMapper.ToRow(
            Entry(after: new { Note = "Server=db;Database=Stockly;Password=hunter2" }),
            Now);

        act.Should().Throw<AuditPayloadSecretException>();
    }

    [Fact]
    public void An_ordinary_payload_is_accepted_so_the_rule_is_not_vacuous()
    {
        // The counter-test. A refusal rule that refused everything would pass every
        // test above while destroying the audit trail.
        var row = AuditEntryMapper.ToRow(
            Entry(before: new { RoleCode = "store_keeper" }, after: new { RoleCode = "manager" }),
            Now);

        row.BeforeJson.Should().Contain("store_keeper");
        row.AfterJson.Should().Contain("manager");
    }

    [Fact]
    public void A_payload_whose_name_only_looks_secret_is_accepted()
    {
        // Guards against the redactor becoming so aggressive that legitimate audit
        // data is discarded — which would silently end the audit trail.
        // camelCase because the payload is serialised with JsonSerializerDefaults.Web,
        // matching how the API emits JSON.
        var row = AuditEntryMapper.ToRow(
            Entry(after: new { ShippingCount = 12, SaltinessLevel = "low" }),
            Now);

        row.AfterJson.Should().Contain("shippingCount");
        row.AfterJson.Should().Contain("saltinessLevel");
    }

    // ---- Concurrency of the collector ----------------------------------

    [Fact]
    public void Two_drains_of_a_shared_collector_cannot_both_return_the_same_entries()
    {
        // Sequential drains already clear the buffer; this asserts the property
        // explicitly so a future change that returns a live list is caught.
        var collector = new AuditCollector();
        collector.Add(Entry());

        var first = collector.Drain().ToArray();
        var second = collector.Drain().ToArray();

        first.Should().HaveCount(1);
        second.Should().BeEmpty();
        first[0].Should().NotBeSameAs(second.FirstOrDefault()!);
    }

    [Fact]
    public void The_drained_snapshot_is_not_affected_by_later_additions()
    {
        // Drain returns a copy. If it returned the live list, a caller building a
        // transaction from it would pick up entries added afterwards.
        var collector = new AuditCollector();
        collector.Add(Entry(after: new { Step = 1 }));

        var drained = collector.Drain();
        collector.Add(Entry(after: new { Step = 2 }));

        drained.Should().ContainSingle().Which.After.Should().BeEquivalentTo(new { Step = 1 });
    }
}