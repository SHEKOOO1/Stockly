namespace Stockly.Application.Common.Errors;

/// <summary>
/// The 27 business error codes of <c>BUSINESS_RULES.md</c> §14.
/// <para>
/// The count is a contract: adding a code is additive, removing or repurposing
/// one is a breaking change and needs a decision record. The wire string, the
/// HTTP status and the problem <c>type</c> URI are part of the public API
/// contract (<c>API_CONVENTIONS.md</c> §6.2) and must not drift.
/// </para>
/// <para>
/// Values are explicit integers rather than ordinals so a code can be inserted
/// later without silently changing another code's number.
/// </para>
/// </summary>
public enum BusinessErrorCode
{
    // --- 401 -------------------------------------------------------------
    /// <summary>Login failed (all causes).</summary>
    InvalidCredentials = 1001,

    /// <summary>
    /// Reserved, <strong>never emitted</strong>. Lockout is deliberately
    /// indistinguishable from a wrong password at the API surface; the code is
    /// kept so the audit action <c>auth.login.locked</c> and the operator UI
    /// have a stable name.
    /// </summary>
    AccountLocked = 1002,

    /// <summary>Session idle/absolute timeout.</summary>
    SessionExpired = 1003,

    /// <summary>Session revoked.</summary>
    SessionRevoked = 1004,

    /// <summary>Signature/claims failure.</summary>
    TokenInvalid = 1005,

    // --- 400 -------------------------------------------------------------
    /// <summary>Field validation errors.</summary>
    ValidationFailed = 2001,

    /// <summary>Unrecognised property (mass-assignment defence).</summary>
    UnknownField = 2002,

    /// <summary>Batch-tracked product without a batch.</summary>
    BatchRequired = 2003,

    // --- 403 -------------------------------------------------------------
    /// <summary>Known resource, missing permission.</summary>
    PermissionDenied = 3001,

    /// <summary>Feature not entitled (plan).</summary>
    SubscriptionRequired = 3002,

    /// <summary>Tenant is read-only (expired/past due).</summary>
    SubscriptionReadOnly = 3003,

    /// <summary>Approver is the requester / lacks permission.</summary>
    ApprovalForbidden = 3004,

    // --- 404 -------------------------------------------------------------
    /// <summary>Missing, or outside tenant/warehouse scope.</summary>
    NotFound = 4001,

    // --- 409 -------------------------------------------------------------
    /// <summary>Guarded balance update rejected.</summary>
    InsufficientStock = 5001,

    /// <summary>Wrong status for posting.</summary>
    DocumentNotPostable = 5002,

    /// <summary>Receiving/issuing an expired lot.</summary>
    LotExpired = 5003,

    /// <summary>Received quantity beyond tolerance.</summary>
    OverReceiptExceeded = 5004,

    /// <summary>Count/Users/Warehouses/Storage limit reached.</summary>
    UsageLimitReached = 5005,

    /// <summary>Document is not approved.</summary>
    ApprovalRequired = 5006,

    /// <summary>Cannot remove the last tenant owner.</summary>
    LastOwnerProtected = 5007,

    /// <summary>Same idempotency key, different payload.</summary>
    IdempotencyConflict = 5008,

    // --- 412 -------------------------------------------------------------
    /// <summary><c>rowversion</c> / <c>If-Match</c> mismatch.</summary>
    ConcurrencyConflict = 6001,

    // --- 413 -------------------------------------------------------------
    /// <summary>Body/limit exceeded.</summary>
    PayloadTooLarge = 7001,

    // --- 415 -------------------------------------------------------------
    /// <summary>File/content type rejected.</summary>
    UnsupportedMediaType = 8001,

    // --- 428 -------------------------------------------------------------
    /// <summary><c>MustChangePin</c> is set; change the PIN before any warehouse operation.</summary>
    PinChangeRequired = 9001,

    /// <summary><c>If-Match</c> missing on an endpoint that requires it.</summary>
    PreconditionRequired = 9002,

    // --- 429 -------------------------------------------------------------
    /// <summary>Throttled (with <c>Retry-After</c>).</summary>
    RateLimited = 10001,
}

/// <summary>
/// The immutable metadata the API contract pins for each code: the wire string,
/// the HTTP status, the RFC 9457 problem <c>type</c> and a safe human title.
/// </summary>
public static class BusinessErrorCodes
{
    /// <summary>Base URI for the RFC 9457 <c>type</c> member.</summary>
    public const string ProblemTypeBase = "https://stockly.dev/problems/";

    /// <summary>
    /// Codes that exist as documented names but are never placed in a response.
    /// Emitting <see cref="BusinessErrorCode.AccountLocked"/> would tell an
    /// attacker that the identifier exists and is locked, which is exactly the
    /// account-enumeration oracle §4.1 forbids.
    /// </summary>
    public static IReadOnlySet<BusinessErrorCode> ReservedCodes { get; } =
        new HashSet<BusinessErrorCode> { BusinessErrorCode.AccountLocked };

    private static readonly Dictionary<BusinessErrorCode, ErrorContract> Map =
        new()
        {
            [BusinessErrorCode.InvalidCredentials] = new(
                "invalid_credentials", 401, "Invalid credentials"),
            [BusinessErrorCode.AccountLocked] = new(
                "account_locked", 401, "Account locked"),
            [BusinessErrorCode.SessionExpired] = new(
                "session_expired", 401, "Session expired"),
            [BusinessErrorCode.SessionRevoked] = new(
                "session_revoked", 401, "Session revoked"),
            [BusinessErrorCode.TokenInvalid] = new(
                "token_invalid", 401, "Invalid token"),

            [BusinessErrorCode.ValidationFailed] = new(
                "validation_failed", 400, "Validation failed"),
            [BusinessErrorCode.UnknownField] = new(
                "unknown_field", 400, "Unknown field"),
            [BusinessErrorCode.BatchRequired] = new(
                "batch_required", 400, "Batch required"),

            [BusinessErrorCode.PermissionDenied] = new(
                "permission_denied", 403, "Permission denied"),
            [BusinessErrorCode.SubscriptionRequired] = new(
                "subscription_required", 403, "Subscription required"),
            [BusinessErrorCode.SubscriptionReadOnly] = new(
                "subscription_read_only", 403, "Subscription is read-only"),
            [BusinessErrorCode.ApprovalForbidden] = new(
                "approval_forbidden", 403, "Approval forbidden"),

            [BusinessErrorCode.NotFound] = new(
                "not_found", 404, "Not found"),

            [BusinessErrorCode.InsufficientStock] = new(
                "insufficient_stock", 409, "Insufficient stock"),
            [BusinessErrorCode.DocumentNotPostable] = new(
                "document_not_postable", 409, "Document not postable"),
            [BusinessErrorCode.LotExpired] = new(
                "lot_expired", 409, "Lot expired"),
            [BusinessErrorCode.OverReceiptExceeded] = new(
                "over_receipt_exceeded", 409, "Over-receipt exceeded"),
            [BusinessErrorCode.UsageLimitReached] = new(
                "usage_limit_reached", 409, "Usage limit reached"),
            [BusinessErrorCode.ApprovalRequired] = new(
                "approval_required", 409, "Approval required"),
            [BusinessErrorCode.LastOwnerProtected] = new(
                "last_owner_protected", 409, "Last owner protected"),
            [BusinessErrorCode.IdempotencyConflict] = new(
                "idempotency_conflict", 409, "Idempotency conflict"),

            [BusinessErrorCode.ConcurrencyConflict] = new(
                "concurrency_conflict", 412, "Concurrency conflict"),

            [BusinessErrorCode.PayloadTooLarge] = new(
                "payload_too_large", 413, "Payload too large"),

            [BusinessErrorCode.UnsupportedMediaType] = new(
                "unsupported_media_type", 415, "Unsupported media type"),

            [BusinessErrorCode.PinChangeRequired] = new(
                "pin_change_required", 428, "PIN change required"),
            [BusinessErrorCode.PreconditionRequired] = new(
                "precondition_required", 428, "Precondition required"),

            [BusinessErrorCode.RateLimited] = new(
                "rate_limited", 429, "Too many requests"),
        };

    /// <summary>The complete documented catalogue. 27 entries.</summary>
    public static IReadOnlyCollection<BusinessErrorCode> All { get; } = Map.Keys.ToArray();

    /// <summary>Resolves the pinned contract for a code.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The code is not in the documented catalogue.</exception>
    public static ErrorContract Get(BusinessErrorCode code) =>
        Map.TryGetValue(code, out var contract)
            ? contract
            : throw new ArgumentOutOfRangeException(
                nameof(code), code, "Not a documented business error code.");

    /// <summary>True when the code may legally appear in a response body.</summary>
    public static bool IsEmittable(BusinessErrorCode code) =>
        !ReservedCodes.Contains(code);

    /// <summary>RFC 9457 <c>type</c> URI for a code, e.g. <c>…/problems/insufficient-stock</c>.</summary>
    public static string GetProblemType(BusinessErrorCode code) =>
        Get(code).ProblemType;

    /// <summary>
    /// <c>snake_case</c> → <c>kebab-case</c>, used only for the <c>type</c> URI
    /// slug so the wire string stays exactly as documented.
    /// </summary>
    public static string ToKebabCase(string wireName) =>
        wireName.Replace('_', '-');
}