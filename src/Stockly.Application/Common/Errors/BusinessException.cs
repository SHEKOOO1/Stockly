namespace Stockly.Application.Common.Errors;

/// <summary>
/// The single exception the application layer throws to produce a documented
/// error response.
/// <para>
/// It carries a <see cref="BusinessErrorCode"/> and nothing else that could leak.
/// There is deliberately no <c>Exception</c> reference, no inner-exception
/// chaining for client purposes and no free-form type name: an endpoint can throw
/// this and the resulting body is safe by construction (S22,
/// <c>ErrorSafetyTests</c>).
/// </para>
/// <para>
/// Anything <em>not</em> modelled here is an unexpected failure and becomes a
/// trace-id-only <c>500</c>.
/// </para>
/// </summary>
public sealed class BusinessException : Exception
{
    /// <summary>Creates a business failure.</summary>
    /// <param name="code">A documented, non-reserved code.</param>
    /// <param name="detail">
    /// A caller-safe sentence. It must name only the caller's own data; §6.2
    /// forbids SQL, stack frames, internal type names, other tenants' data and
    /// connection details.
    /// </param>
    /// <param name="errors">Optional field-path → codes map for validation.</param>
    public BusinessException(
        BusinessErrorCode code,
        string? detail = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? errors = null)
        : base(detail ?? $"{(int)code}")
    {
        if (!BusinessErrorCodes.IsEmittable(code))
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "Reserved codes must never be thrown: they are an enumeration oracle.");
        }

        Code = code;
        Detail = detail ?? string.Empty;
        Errors = errors ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    }

    /// <summary>The documented code this failure is emitted with.</summary>
    public BusinessErrorCode Code { get; }

    /// <summary>A caller-safe sentence, or empty when there is nothing safe to say.</summary>
    public string Detail { get; }

    /// <summary>Field-path → error codes. Values are codes, never sentences.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    /// <summary>The HTTP status for <see cref="Code"/>.</summary>
    public int HttpStatus => BusinessErrorCodes.Get(Code).HttpStatus;

    // -- Shorthand factories for the codes raised from more than one layer ------

    /// <summary>
    /// <c>404</c> for anything missing <em>or</em> outside the caller's
    /// tenant/warehouse scope. §5.2 requires <c>404</c> rather than <c>403</c>
    /// so an id cannot be probed, so this is deliberately used for both.
    /// </summary>
    public static BusinessException NotFound(string? detail = null) =>
        new(BusinessErrorCode.NotFound, detail);

    /// <summary>Field validation failure (<c>400</c>).</summary>
    public static BusinessException Validation(
        IReadOnlyDictionary<string, IReadOnlyList<string>> errors,
        string? detail = null) =>
        new(BusinessErrorCode.ValidationFailed, detail ?? "One or more fields are invalid.", errors);

    /// <summary>
    /// The uniform authentication failure. Every cause — unknown user, wrong
    /// password, revoked session, wrong tenant — produces this same body, because
    /// §4.1 requires them to be indistinguishable.
    /// </summary>
    public static BusinessException InvalidCredentials() =>
        new(BusinessErrorCode.InvalidCredentials, "Invalid credentials.");

    /// <summary>Missing or invalid token (<c>401</c>).</summary>
    public static BusinessException TokenInvalid(string? detail = null) =>
        new(BusinessErrorCode.TokenInvalid, detail ?? "The access token is missing or invalid.");

    /// <summary>
    /// <c>403</c> — only for a resource the caller is *allowed to know exists*
    /// but lacks permission for. Anything outside scope uses
    /// <see cref="NotFound"/> instead.
    /// </summary>
    public static BusinessException PermissionDenied(string? detail = null) =>
        new(BusinessErrorCode.PermissionDenied, detail ?? "Permission denied.");

    /// <summary>Throttled (<c>429</c>). <c>Retry-After</c> is added by the writer.</summary>
    public static BusinessException RateLimited() =>
        new(BusinessErrorCode.RateLimited, "Too many requests.");

    /// <summary>
    /// The uniform PIN-terminal failure. Wrong PIN, unknown membership, locked
    /// PIN and disabled membership all produce this, per §4.2.
    /// </summary>
    public static BusinessException TerminalVerificationFailed() =>
        new(BusinessErrorCode.InvalidCredentials, "Invalid credentials.");
}