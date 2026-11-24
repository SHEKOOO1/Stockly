using System.Text.Json;
using FluentAssertions;
using Stockly.Application.Common.Errors;
using Xunit;

namespace Stockly.UnitTests.Common.Errors;

/// <summary>
/// Guards the error contract in <c>BUSINESS_RULES.md</c> §14 and
/// <c>API_CONVENTIONS.md</c> §6.2–§6.3.
/// <para>
/// No database is needed: the contract is a static table, and its drift would only
/// be observable at runtime, in production.
/// </para>
/// </summary>
public sealed class BusinessErrorCodeTests
{
    [Fact]
    public void Catalogue_contains_exactly_the_27_documented_codes()
    {
        BusinessErrorCodes.All.Should().HaveCount(27);
    }

    [Fact]
    public void Every_documented_code_is_present()
    {
        // The literal list from BUSINESS_RULES.md §14. If a code is removed or
        // repurposed this test fails, which is the intended breaking-change alarm.
        string[] documented =
        [
            "invalid_credentials", "account_locked", "pin_change_required",
            "precondition_required", "session_expired", "session_revoked",
            "token_invalid", "permission_denied", "not_found", "validation_failed",
            "unknown_field", "concurrency_conflict", "insufficient_stock",
            "document_not_postable", "lot_expired", "batch_required",
            "over_receipt_exceeded", "subscription_required", "subscription_read_only",
            "usage_limit_reached", "approval_required", "approval_forbidden",
            "last_owner_protected", "idempotency_conflict", "rate_limited",
            "payload_too_large", "unsupported_media_type",
        ];

        var actual = BusinessErrorCodes.All
            .Select(c => BusinessErrorCodes.Get(c).WireName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        actual.Should().BeEquivalentTo(documented);
    }

    [Theory]
    [InlineData(BusinessErrorCode.AccountLocked, "account_locked", 401)]
    [InlineData(BusinessErrorCode.InvalidCredentials, "invalid_credentials", 401)]
    [InlineData(BusinessErrorCode.PinChangeRequired, "pin_change_required", 428)]
    [InlineData(BusinessErrorCode.PreconditionRequired, "precondition_required", 428)]
    [InlineData(BusinessErrorCode.PermissionDenied, "permission_denied", 403)]
    [InlineData(BusinessErrorCode.NotFound, "not_found", 404)]
    [InlineData(BusinessErrorCode.ValidationFailed, "validation_failed", 400)]
    [InlineData(BusinessErrorCode.UnknownField, "unknown_field", 400)]
    [InlineData(BusinessErrorCode.ConcurrencyConflict, "concurrency_conflict", 412)]
    [InlineData(BusinessErrorCode.InsufficientStock, "insufficient_stock", 409)]
    [InlineData(BusinessErrorCode.RateLimited, "rate_limited", 429)]
    [InlineData(BusinessErrorCode.PayloadTooLarge, "payload_too_large", 413)]
    [InlineData(BusinessErrorCode.UnsupportedMediaType, "unsupported_media_type", 415)]
    public void Code_is_pinned_to_its_wire_string_and_status(
            BusinessErrorCode code, string wireName, int status)
    {
        var contract = BusinessErrorCodes.Get(code);

        contract.WireName.Should().Be(wireName);
        contract.HttpStatus.Should().Be(status);
    }

    [Fact]
    public void Wire_names_are_unique()
    {
        BusinessErrorCodes.All
            .Select(c => BusinessErrorCodes.Get(c).WireName)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Exactly_one_code_is_reserved_and_never_emitted()
    {
        BusinessErrorCodes.ReservedCodes
            .Should().BeEquivalentTo([BusinessErrorCode.AccountLocked]);

        BusinessErrorCodes.All.Count(BusinessErrorCodes.IsEmittable)
            .Should().Be(26, "26 of the 27 documented codes may be emitted");
    }

    [Fact]
    public void Emitting_a_reserved_code_throws_rather_than_leaking_it()
    {
        // account_locked would tell an attacker the identifier exists and is
        // locked, which is exactly the enumeration oracle §4.1 forbids.
        var act = () => ApiProblem.Create(BusinessErrorCode.AccountLocked, "trace-1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*reserved*");
    }

    [Fact]
    public void Throwing_a_reserved_code_is_rejected_at_the_construction_site()
    {
        var act = () => new BusinessException(BusinessErrorCode.AccountLocked);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Problem_type_uri_is_the_documented_base_plus_a_kebab_slug()
    {
        BusinessErrorCodes.GetProblemType(BusinessErrorCode.InsufficientStock)
            .Should().Be("https://stockly.dev/problems/insufficient-stock");

        BusinessErrorCodes.GetProblemType(BusinessErrorCode.OverReceiptExceeded)
            .Should().Be("https://stockly.dev/problems/over-receipt-exceeded");

        BusinessErrorCodes.GetProblemType(BusinessErrorCode.SubscriptionReadOnly)
            .Should().Be("https://stockly.dev/problems/subscription-read-only");
    }

    [Fact]
    public void Titles_are_short_and_carry_no_internal_detail()
    {
        foreach (var code in BusinessErrorCodes.All)
        {
            var title = BusinessErrorCodes.Get(code).Title;
            var lower = title.ToLowerInvariant();

            title.Should().NotBeNullOrWhiteSpace();
            lower.Should().NotContain("stockly");
            lower.Should().NotContain("exception");
            lower.Should().NotContain(" at ");
            lower.Should().NotContain("select");
            lower.Should().NotContain("[]");
            title.Length.Should().BeLessThan(60, "'{0}' is too long to be a title", title);
        }
    }

    [Fact]
    public void An_undocumented_code_is_rejected_rather_than_silently_mapped()
    {
        var act = () => BusinessErrorCodes.Get((BusinessErrorCode)999999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

/// <summary>
/// RFC 9457 body shape and S22 error safety.
/// </summary>
public sealed class ApiProblemTests
{
    [Fact]
    public void Body_carries_the_documented_members()
    {
        var problem = ApiProblem.Create(
            BusinessErrorCode.InsufficientStock,
            traceId: "0HN7-abc",
            detail: "Product 'RICE-25KG' has 3 available but 5 were requested.",
            instance: "/api/v1/inventory/stock-out");

        using var json = JsonDocument.Parse(Stockly.Api.Common.ProblemDetailsWriter.Serialize(problem));
        var root = json.RootElement;

        root.GetProperty("type").GetString()
            .Should().Be("https://stockly.dev/problems/insufficient-stock");
        root.GetProperty("title").GetString().Should().Be("Insufficient stock");
        root.GetProperty("status").GetInt32().Should().Be(409);
        root.GetProperty("detail").GetString().Should().Contain("RICE-25KG");
        root.GetProperty("instance").GetString().Should().Be("/api/v1/inventory/stock-out");
        root.GetProperty("traceId").GetString().Should().Be("0HN7-abc");
        root.GetProperty("code").GetString().Should().Be("insufficient_stock");
    }

    [Fact]
    public void Trace_id_is_always_present_so_a_user_can_quote_it()
    {
        var problem = ApiProblem.Create(BusinessErrorCode.NotFound, traceId: "abc-123");

        problem.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Errors_are_field_path_keyed_and_carry_codes_not_sentences()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>>
        {
            ["lines[0].quantity"] = ["insufficient_stock"],
            ["documentNumber"] = ["required", "too_long"],
        };

        var problem = ApiProblem.Create(
            BusinessErrorCode.ValidationFailed, "trace", "Invalid.", "/api/v1/x", errors);

        using var json = JsonDocument.Parse(Stockly.Api.Common.ProblemDetailsWriter.Serialize(problem));
        var serialized = json.RootElement.GetProperty("errors");

        serialized.GetProperty("lines[0].quantity")[0].GetString().Should().Be("insufficient_stock");
        serialized.GetProperty("documentNumber")[1].GetString().Should().Be("too_long");
    }

    [Fact]
    public void An_empty_errors_map_is_omitted_rather_than_serialised_as_an_empty_object()
    {
        var problem = ApiProblem.Create(BusinessErrorCode.NotFound, "trace");

        Stockly.Api.Common.ProblemDetailsWriter.Serialize(problem)
            .Should().NotContain("\"errors\"");
    }

    [Fact]
    public void Media_type_is_the_rfc_9457_value()
    {
        ApiProblem.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public void An_internal_error_never_carries_an_exception_message()
    {
        // §6.3: "Never return 500 with an exception message."
        var problem = ApiProblem.Internal("trace-9", "/api/v1/x");

        problem.Status.Should().Be(500);
        problem.Title.Should().Be("Internal error");
        problem.Detail.Should().NotContain("Exception");
        problem.Detail.Should().Contain("traceId");
    }

    [Fact]
    public void A_dependency_failure_never_names_the_dependency()
    {
        var problem = ApiProblem.DependencyUnavailable("trace-9", "/api/v1/x");
        var detail = problem.Detail.ToLowerInvariant();

        problem.Status.Should().Be(503);
        detail.Should().NotContain("sql");
        detail.Should().NotContain("connection");
        detail.Should().NotContain("host");
        detail.Should().NotContain("database");
    }

    [Fact]
    public void No_serialised_problem_carries_a_null_code()
    {
        // §6.2 requires `code` to be a documented string. 500 and 503 have no
        // documented code, so `code` is absent rather than invented.
        var body500 = Stockly.Api.Common.ProblemDetailsWriter.Serialize(ApiProblem.Internal("t"));
        var body503 = Stockly.Api.Common.ProblemDetailsWriter.Serialize(ApiProblem.DependencyUnavailable("t"));

        body500.Should().NotContain("\"code\"");
        body503.Should().NotContain("\"code\"");
    }
}

/// <summary>
/// The exception type must not be a channel for server internals.
/// </summary>
public sealed class BusinessExceptionTests
{
    [Fact]
    public void Status_comes_from_the_catalogue_not_the_caller()
    {
        new BusinessException(BusinessErrorCode.NotFound).HttpStatus.Should().Be(404);
        new BusinessException(BusinessErrorCode.RateLimited).HttpStatus.Should().Be(429);
    }

    [Fact]
    public void Not_found_is_the_shorthand_for_both_missing_and_out_of_scope()
    {
        // §5.2: 404 rather than 403 so an id cannot be probed.
        var ex = BusinessException.NotFound();

        ex.Code.Should().Be(BusinessErrorCode.NotFound);
        ex.HttpStatus.Should().Be(404);
    }

    [Fact]
    public void Invalid_credentials_has_one_shape_for_every_authentication_failure()
    {
        // §4.1: identical body, status and approximate timing whether the account
        // is missing, the password is wrong, or the account is locked.
        BusinessException.InvalidCredentials().Code
            .Should().Be(BusinessErrorCode.InvalidCredentials);

        BusinessException.TerminalVerificationFailed().Code
            .Should().Be(BusinessErrorCode.InvalidCredentials);

        BusinessException.TerminalVerificationFailed().Detail
            .Should().Be(BusinessException.InvalidCredentials().Detail);
    }

    [Fact]
    public void Validation_always_carries_a_field_map()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>>
        {
            ["quantity"] = ["required"],
        };

        var ex = BusinessException.Validation(errors);

        ex.Code.Should().Be(BusinessErrorCode.ValidationFailed);
        ex.Errors.Should().ContainKey("quantity");
        ex.Detail.Should().NotBeNullOrWhiteSpace();
    }
}