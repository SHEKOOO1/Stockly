using System.Net;

namespace Stockly.Application.Common.Errors;

/// <summary>
/// An RFC 9457 <c>application/problem+json</c> body, plus Stockly's two extra
/// members (<c>code</c> and <c>errors</c>) from <c>API_CONVENTIONS.md</c> §6.2.
/// <para>
/// The type deliberately has no <c>Exception</c> on it. A problem detail is
/// constructed from a <see cref="BusinessErrorCode"/> and a caller-supplied
/// <c>detail</c> string; it cannot carry a stack trace, SQL text or an internal
/// type name into the response (S22, <c>ErrorSafetyTests</c>).
/// </para>
/// </summary>
public sealed record ApiProblem
{
    private ApiProblem(ErrorContract contract)
    {
        Code = contract.WireName;
        Title = contract.Title;
        Status = contract.HttpStatus;
        Type = contract.ProblemType;
    }

    /// <summary>
    /// RFC 9457 <c>type</c>. Always set from the catalogue by the private
    /// constructor; a caller cannot invent one.
    /// </summary>
    public string Type { get; init; }

    /// <summary>RFC 9457 <c>title</c> — short, safe, from the catalogue.</summary>
    public string Title { get; init; }

    /// <summary>RFC 9457 <c>status</c>. Always set from the catalogue.</summary>
    public int Status { get; init; }

    /// <summary>
    /// RFC 9457 <c>detail</c> — a caller-safe sentence. Empty rather than
    /// omitted, so the shape of the body never varies with the failure.
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>RFC 9457 <c>instance</c> — the request path, never the full URL.</summary>
    public string Instance { get; init; } = string.Empty;

    /// <summary>Always present so a user can quote it in a support request.</summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// The stable machine code from <c>BUSINESS_RULES.md</c> §14, or
    /// <see langword="null"/> for <c>500</c>/<c>503</c>, which §6.3 defines as
    /// statuses but §14 assigns no code. The catalogue count is a contract
    /// ("adding a code is additive"), so no code was invented here — see the
    /// open question in <c>DEVELOPMENT_STATUS.md</c>.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Field-path → error codes. Values are codes, never sentences, so the client
    /// can localise them and no server text leaks. <see langword="null"/> rather
    /// than empty so a simple failure is not serialised with an empty
    /// <c>"errors": {}</c>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors { get; init; }

    /// <summary>The media type RFC 9457 requires.</summary>
    public const string ContentType = "application/problem+json";

    /// <summary>
    /// Builds a problem for a documented code.
    /// </summary>
    /// <param name="code">The documented code. Must not be reserved.</param>
    /// <param name="traceId">Correlation / trace id surfaced to the caller.</param>
    /// <param name="detail">
    /// A safe, caller-facing sentence. Pass <see langword="null"/> or an empty
    /// string when there is nothing safe to say.
    /// </param>
    /// <param name="instance">The request path.</param>
    /// <param name="errors">Optional field-path → codes map.</param>
    /// <exception cref="InvalidOperationException">
    /// The code is reserved (<see cref="BusinessErrorCode.AccountLocked"/>).
    /// Reserved codes are a compile-time-adjacent trap, not a runtime detail:
    /// emitting one would be an enumeration oracle, so this throws rather than
    /// quietly returning a body.
    /// </exception>
    public static ApiProblem Create(
        BusinessErrorCode code,
        string traceId,
        string? detail = null,
        string? instance = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? errors = null)
    {
        if (!BusinessErrorCodes.IsEmittable(code))
        {
            throw new InvalidOperationException(
                $"'{code}' is reserved and must never be emitted; a caller must use " +
                "a code that does not reveal why authentication failed.");
        }

        var contract = BusinessErrorCodes.Get(code);
        return new ApiProblem(contract)
        {
            Detail = detail ?? string.Empty,
            Instance = instance ?? string.Empty,
            TraceId = traceId,
            Errors = errors is null || errors.Count == 0
                ? null
                : errors,
        };
    }

    /// <summary>
    /// The generic problem for an unexpected failure. §6.3 forbids a <c>500</c>
    /// carrying an exception message, so this carries only a trace id and a fixed
    /// sentence; the exception stays server-side.
    /// </summary>
    public static ApiProblem Internal(string traceId, string? instance = null) =>
        new(new ErrorContract(null, (int)HttpStatusCode.InternalServerError, "Internal error"))
        {
            Detail = "An unexpected error occurred. Quote the traceId when reporting this.",
            Instance = instance ?? string.Empty,
            TraceId = traceId,
        };

    /// <summary>
    /// The generic problem for an unavailable dependency (§6.3 <c>503</c>).
    /// </summary>
    public static ApiProblem DependencyUnavailable(string traceId, string? instance = null) =>
        new(new ErrorContract(
            null,
            (int)HttpStatusCode.ServiceUnavailable,
            "Dependency unavailable"))
        {
            // Deliberately does not name the dependency: naming a database host or
            // connection pool is an infrastructure disclosure (S22).
            Detail = "A required dependency is unavailable. Try again shortly.",
            Instance = instance ?? string.Empty,
            TraceId = traceId,
        };
}

/// <summary>
/// The wire name, status and title of one problem. A <see langword="null"/>
/// <see cref="WireName"/> means §14 defines no code for that status.
/// </summary>
/// <param name="WireName">The stable machine string, e.g. <c>insufficient_stock</c>.</param>
/// <param name="HttpStatus">The HTTP status the code is emitted with.</param>
/// <param name="Title">
/// A short, safe human string. It must never contain SQL, a stack frame, an
/// internal type name, a host name or any other tenant's data.
/// </param>
public sealed record ErrorContract(string? WireName, int HttpStatus, string Title)
{
    /// <summary>The RFC 9457 <c>type</c> URI for this contract.</summary>
    public string ProblemType => WireName is null
        ? BusinessErrorCodes.ProblemTypeBase + "http-failure"
        : BusinessErrorCodes.ProblemTypeBase + BusinessErrorCodes.ToKebabCase(WireName);
}