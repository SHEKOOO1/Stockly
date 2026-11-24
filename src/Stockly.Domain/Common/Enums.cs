// Shared primitive types and the enum vocabulary of the V1 schema.
//
// Every enum here is persisted as `smallint` with a `CHECK` constraint pinning
// the legal values (DATABASE_DESIGN.md §1.9, §3). The numeric values below ARE
// the database values; they are not an EF Core convention and must not be
// renumbered without a decision record, because the values are pinned by CHECK
// constraints and by seeded data.

namespace Stockly.Domain.Common;

// ---------------------------------------------------------------------------
// Identity / session
// ---------------------------------------------------------------------------

/// <summary>identity.AuthSession.SessionScope (ADR-0034).</summary>
public enum SessionScope
{
    /// <summary>`scp = tenant`. TenantId and MembershipId are both NOT NULL.</summary>
    Tenant = 1,

    /// <summary>`scp = platform`. TenantId and MembershipId are both NULL.</summary>
    Platform = 2,
}

/// <summary>identity.AuthSession.Status.</summary>
public enum SessionStatus
{
    Active = 1,
    Expired = 2,
    Revoked = 3,
    Superseded = 4,
}

/// <summary>identity.AuthSession.AuthenticationMethod.</summary>
public enum AuthenticationMethod
{
    Password = 1,
    Pin = 2,
}

/// <summary>identity.RefreshToken.Status.</summary>
public enum RefreshTokenStatus
{
    Active = 1,
    Used = 2,
    Revoked = 3,
    ReplacedByReuse = 4,
}

/// <summary>identity.TenantMembership.Status.</summary>
public enum MembershipStatus
{
    Active = 1,
    Suspended = 2,
    Disabled = 3,
}

/// <summary>identity.Role.RoleType.</summary>
public enum RoleType
{
    System = 1,
    Custom = 2,
}

/// <summary>identity.MembershipRole.ScopeType.</summary>
public enum MembershipRoleScopeType
{
    AllAssignedWarehouses = 1,
    SpecificWarehouses = 2,
}

/// <summary>identity.Device.Type.</summary>
public enum DeviceType
{
    Kiosk = 1,
    Tablet = 2,
    Desktop = 3,
    Handheld = 4,
}

/// <summary>identity.Device.Status.</summary>
public enum DeviceStatus
{
    Active = 1,
    Suspended = 2,
    Revoked = 3,
}

/// <summary>identity.Warehouse.Type. There is deliberately NO in-transit value (ADR-0047).</summary>
public enum WarehouseType
{
    Main = 1,
    Storage = 2,
    Kitchen = 3,
    Cold = 4,
    Vehicle = 5,
    Quarantine = 6,
}

/// <summary>identity.LoginAttempt.Method.</summary>
public enum LoginAttemptMethod
{
    Password = 1,
    Pin = 2,
    Refresh = 3,
    Reset = 4,
}

/// <summary>identity.LoginAttempt.Outcome.</summary>
public enum LoginAttemptOutcome
{
    Success = 1,
    Invalid = 2,
    Locked = 3,
    RateLimited = 4,

    /// <summary>The account does not exist. Never distinguishable to the client (SECURITY_ARCHITECTURE §4.1).</summary>
    UnknownAccount = 5,
}

// ---------------------------------------------------------------------------
// Platform / SaaS
// ---------------------------------------------------------------------------

/// <summary>identity.Tenant.Status.</summary>
public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 3,
}

/// <summary>platform.Subscription.Status.</summary>
public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Cancelled = 4,
    Expired = 5,
}

/// <summary>platform.PlanFeature.LimitType.</summary>
public enum LimitType
{
    None = 0,
    Boolean = 1,
    Count = 2,
    Users = 3,
    Warehouses = 4,
    StorageGb = 5,
}

// ---------------------------------------------------------------------------
// Master data
// ---------------------------------------------------------------------------

/// <summary>master.Unit.UnitType.</summary>
public enum UnitType
{
    Count = 1,
    Weight = 2,
    Volume = 3,
    Length = 4,
    Time = 5,
}

/// <summary>master.ProductBarcode.Symbology (ADR-0043). An unknown value is stored as Code128.</summary>
public enum BarcodeSymbology
{
    Ean13 = 1,
    Ean8 = 2,
    UpcA = 3,
    Code128 = 4,
    Qr = 5,
    DataMatrix = 6,
}

// ---------------------------------------------------------------------------
// Inventory
// ---------------------------------------------------------------------------

/// <summary>inventory.StockDocument.MovementType (also master.ReasonCode.MovementType, inventory.StockTransaction.MovementType).</summary>
public enum MovementType
{
    StockIn = 1,
    StockOut = 2,
    Return = 3,
    Transfer = 4,
    Adjustment = 5,
    Waste = 6,
    Count = 7,
}

/// <summary>inventory.StockTransaction.Direction. A direction column is preferred over a signed quantity (DATABASE_DESIGN §3).</summary>
public enum MovementDirection
{
    In = 1,
    Out = 2,
}

/// <summary>inventory.StockDocument.Status.</summary>
public enum StockDocumentStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3,
    Reversed = 4,
}

/// <summary>inventory.StockDocument.SourceType. A tag for the API, not the referential link (ADR-0038).</summary>
public enum StockDocumentSourceType
{
    None = 0,
    GoodsReceipt = 1,
    StockCount = 2,
    EventConsumption = 3,
}

/// <summary>inventory.StockCount.Status.</summary>
public enum StockCountStatus
{
    Draft = 1,
    Counting = 2,
    Submitted = 3,
    Posted = 4,
    Cancelled = 5,
}

/// <summary>inventory.StockCount.ScopeType.</summary>
public enum StockCountScopeType
{
    Full = 1,
    Category = 2,
    Selected = 3,
    ExpiryBased = 4,
}

/// <summary>inventory.StockCountLine.Resolution.</summary>
public enum StockCountResolution
{
    None = 1,
    Accepted = 2,
    Recount = 3,
    Ignored = 4,
}

// ---------------------------------------------------------------------------
// Purchasing
// ---------------------------------------------------------------------------

/// <summary>purchasing.PurchaseRequest.Status.</summary>
public enum PurchaseRequestStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Converted = 5,
    Cancelled = 6,
}

/// <summary>purchasing.PurchaseRequest.Priority.</summary>
public enum PurchasePriority
{
    Normal = 1,
    High = 2,
    Urgent = 3,
}

/// <summary>purchasing.PurchaseOrder.Status.</summary>
public enum PurchaseOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    PartiallyReceived = 4,
    Received = 5,
    Cancelled = 6,
    Closed = 7,
}

/// <summary>purchasing.PurchaseApproval.EntityType.</summary>
public enum ApprovalEntityType
{
    PurchaseRequest = 1,
    PurchaseOrder = 2,
}

/// <summary>purchasing.PurchaseApproval.Decision.</summary>
public enum ApprovalDecision
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
}

/// <summary>purchasing.GoodsReceipt.Status.</summary>
public enum GoodsReceiptStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3,
}

/// <summary>purchasing.ProductPriceHistory.SourceType. Provenance tag only; SourceId is deliberately not an FK (ADR-0038).</summary>
public enum PriceSourceType
{
    PoLine = 1,
    GoodsReceipt = 2,
    Manual = 3,
}

// ---------------------------------------------------------------------------
// Events
// ---------------------------------------------------------------------------

/// <summary>events.Event.Type.</summary>
public enum EventType
{
    Conference = 1,
    Residence = 2,
    Wedding = 3,
    Training = 4,
    Other = 5,
}

/// <summary>events.Event.Status.</summary>
public enum EventStatus
{
    Planning = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}

/// <summary>events.EventRequirement.BasisType.</summary>
public enum RequirementBasisType
{
    Total = 1,
    PerPerson = 2,
    PerPersonPerDay = 3,
    PerPersonPerMeal = 4,
}

// ---------------------------------------------------------------------------
// Food
// ---------------------------------------------------------------------------

/// <summary>food.RecipeCostSnapshot.ValuationMethod.</summary>
public enum ValuationMethod
{
    MovingAverage = 1,
    LastPurchasePrice = 2,
    StandardCost = 3,
}

// ---------------------------------------------------------------------------
// Notifications
// ---------------------------------------------------------------------------

/// <summary>notifications.Notification.Severity.</summary>
public enum NotificationSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}

/// <summary>notifications.NotificationPreference.Channel.</summary>
public enum NotificationChannel
{
    InApp = 1,
    Push = 2,
    Email = 3,
}

/// <summary>files.Attachment.OcrStatus. Text extraction is a reserved V2 capability (ADR-0042).</summary>
public enum OcrStatus
{
    NotApplicable = 0,
    Pending = 1,
    Processed = 2,
    Failed = 3,
}

// ---------------------------------------------------------------------------
// Extensibility
// ---------------------------------------------------------------------------

/// <summary>extensibility.CustomFieldDefinition.FieldType.</summary>
public enum CustomFieldType
{
    Text = 1,
    Number = 2,
    Date = 3,
    Boolean = 4,
    Dropdown = 5,
    MultiSelect = 6,
}

// ---------------------------------------------------------------------------
// Audit
// ---------------------------------------------------------------------------

/// <summary>audit.AuditLog.Category.</summary>
public enum AuditCategory
{
    Security = 1,
    Identity = 2,
    Inventory = 3,
    Purchasing = 4,
    Events = 5,
    Food = 6,
    Administration = 7,
    Platform = 8,
}

/// <summary>audit.AuditLog.Severity.</summary>
public enum AuditSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}

/// <summary>audit.AuditLog.Result.</summary>
public enum AuditResult
{
    Success = 1,
    Failure = 2,
    Denied = 3,
}

// ---------------------------------------------------------------------------
// Ops
// ---------------------------------------------------------------------------

/// <summary>ops.OutboxMessage.Status.</summary>
public enum OutboxStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    DeadLetter = 5,
}
