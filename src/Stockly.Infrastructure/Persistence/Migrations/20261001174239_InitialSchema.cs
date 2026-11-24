using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stockly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "master");

            migrationBuilder.EnsureSchema(
                name: "extensibility");

            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.EnsureSchema(
                name: "food");

            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "AppUser",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PasswordSetAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    IsPlatformAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<short>(type: "smallint", nullable: false),
                    Severity = table.Column<short>(type: "smallint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Result = table.Column<short>(type: "smallint", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.CheckConstraint("CK_AuditLog_Category", "[Category] IN (1,2,3,4,5,6,7,8)");
                    table.CheckConstraint("CK_AuditLog_Result", "[Result] IN (1,2,3)");
                    table.CheckConstraint("CK_AuditLog_Severity", "[Severity] IN (1,2,3)");
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempt",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<short>(type: "smallint", nullable: false),
                    Outcome = table.Column<short>(type: "smallint", nullable: false),
                    IdentifierHash = table.Column<byte[]>(type: "varbinary(900)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnrollmentCodeHash = table.Column<byte[]>(type: "varbinary(900)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempt", x => x.Id);
                    table.CheckConstraint("CK_LoginAttempt_Method", "[Method] IN (1,2,3,4)");
                    table.CheckConstraint("CK_LoginAttempt_Outcome", "[Outcome] IN (1,2,3,4,5)");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Attempts = table.Column<short>(type: "smallint", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessage_Attempts", "[Attempts] >= 0");
                    table.CheckConstraint("CK_OutboxMessage_Status", "[Status] IN (1,2,3,4,5)");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDangerous = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plan",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", fixedLength: true, nullable: false),
                    BillingPeriodDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenant",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultCulture = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateFormat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                    table.CheckConstraint("CK_Tenant_Status", "[Status] IN (1,2,3)");
                });

            migrationBuilder.CreateTable(
                name: "AuthSession",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionScope = table.Column<short>(type: "smallint", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    AuthenticationMethod = table.Column<short>(type: "smallint", nullable: false),
                    PinCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PinVersion = table.Column<int>(type: "int", nullable: true),
                    RefreshFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AbsoluteExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RevokedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSession", x => x.Id);
                    table.CheckConstraint("CK_AuthSession_AbsoluteExpiry", "[AbsoluteExpiresAtUtc] >= [IssuedAtUtc] AND [ExpiresAtUtc] <= [AbsoluteExpiresAtUtc]");
                    table.CheckConstraint("CK_AuthSession_AuthenticationMethod", "[AuthenticationMethod] IN (1,2)");
                    table.CheckConstraint("CK_AuthSession_SessionScope", "([SessionScope] = 2 AND [TenantId] IS NULL AND [MembershipId] IS NULL) OR ([SessionScope] = 1 AND [TenantId] IS NOT NULL AND [MembershipId] IS NOT NULL)");
                    table.CheckConstraint("CK_AuthSession_Status", "[Status] IN (1,2,3,4)");
                    table.ForeignKey(
                        name: "FK_AuthSession_AppUser",
                        column: x => x.UserId,
                        principalSchema: "platform",
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanFeature",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LimitType = table.Column<short>(type: "smallint", nullable: false),
                    LimitValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanFeature", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanFeature_Plan_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "platform",
                        principalTable: "Plan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.UniqueConstraint("AK_Category_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_Category_ParentCategory",
                        columns: x => new { x.ParentCategoryId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Category",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Category_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinition",
                schema: "extensibility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FieldType = table.Column<short>(type: "smallint", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinition", x => x.Id);
                    table.UniqueConstraint("AK_CustomFieldDefinition_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_CustomFieldDefinition_FieldType", "[FieldType] IN (1,2,3,4,5,6)");
                    table.ForeignKey(
                        name: "FK_CustomFieldDefinition_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequence",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<short>(type: "smallint", nullable: false),
                    PeriodKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NextValue = table.Column<long>(type: "bigint", nullable: false),
                    Padding = table.Column<short>(type: "smallint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequence", x => x.Id);
                    table.UniqueConstraint("AK_DocumentSequence_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_DocumentSequence_Padding", "[Padding] BETWEEN 1 AND 18");
                    table.ForeignKey(
                        name: "FK_DocumentSequence_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MealType",
                schema: "food",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultServingsPerDay = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealType", x => x.Id);
                    table.UniqueConstraint("AK_MealType_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_MealType_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "varbinary(900)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetToken", x => x.Id);
                    table.UniqueConstraint("AK_PasswordResetToken_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_PasswordResetToken_AppUser",
                        column: x => x.UserId,
                        principalSchema: "platform",
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PasswordResetToken_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PinCredential",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    MustChangePin = table.Column<bool>(type: "bit", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SetAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SetByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinCredential", x => x.Id);
                    table.UniqueConstraint("AK_PinCredential_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_PinCredential_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReasonCode",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonCode", x => x.Id);
                    table.UniqueConstraint("AK_ReasonCode_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_ReasonCode_MovementType", "[MovementType] IN (1,2,3,4,5,6,7)");
                    table.ForeignKey(
                        name: "FK_ReasonCode_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "varbinary(900)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.UniqueConstraint("AK_RefreshToken_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_RefreshToken_Status", "[Status] IN (1,2,3,4)");
                    table.ForeignKey(
                        name: "FK_RefreshToken_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoleType = table.Column<short>(type: "smallint", nullable: false),
                    AuthorityLevel = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                    table.UniqueConstraint("AK_Role_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Role_AuthorityLevel", "[AuthorityLevel] BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_Role_RoleType", "[RoleType] IN (1,2)");
                    table.ForeignKey(
                        name: "FK_Role_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.Id);
                    table.UniqueConstraint("AK_Subscription_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_Subscription_Plan_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "platform",
                        principalTable: "Plan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscription_Tenant",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionUsage",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LimitValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionUsage", x => x.Id);
                    table.UniqueConstraint("AK_SubscriptionUsage_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_SubscriptionUsage_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentTermsDays = table.Column<int>(type: "int", nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", fixedLength: true, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.Id);
                    table.UniqueConstraint("AK_Supplier_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_Supplier_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantEntitlement",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LimitType = table.Column<short>(type: "smallint", nullable: false),
                    LimitValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrantedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantEntitlement", x => x.Id);
                    table.UniqueConstraint("AK_TenantEntitlement_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_TenantEntitlement_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantMembership",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    IsOwner = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreferredCulture = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMembership", x => x.Id);
                    table.UniqueConstraint("AK_TenantMembership_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_TenantMembership_Status", "[Status] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_TenantMembership_AppUser",
                        column: x => x.UserId,
                        principalSchema: "platform",
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantMembership_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantSetting",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", fixedLength: true, nullable: false),
                    NumberOfDecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    DefaultWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LowStockPolicy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiryWarningDays = table.Column<int>(type: "int", nullable: false),
                    DefaultSessionIdleMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultSessionAbsoluteHours = table.Column<int>(type: "int", nullable: false),
                    PinLength = table.Column<int>(type: "int", nullable: false),
                    RequireReasonCodeForAdjustments = table.Column<bool>(type: "bit", nullable: false),
                    RequireReasonCodeForWaste = table.Column<bool>(type: "bit", nullable: false),
                    AllowNegativeStock = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSetting", x => x.Id);
                    table.UniqueConstraint("AK_TenantSetting_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_TenantSetting_AllowNegativeStock", "[AllowNegativeStock] = 0");
                    table.CheckConstraint("CK_TenantSetting_NumberOfDecimalPlaces", "[NumberOfDecimalPlaces] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_TenantSetting_PinLength", "[PinLength] >= 6");
                    table.ForeignKey(
                        name: "FK_TenantSetting_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unit",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UnitType = table.Column<short>(type: "smallint", nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "tinyint", nullable: false),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unit", x => x.Id);
                    table.UniqueConstraint("AK_Unit_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Unit_DecimalPlaces", "[DecimalPlaces] <= 4");
                    table.CheckConstraint("CK_Unit_UnitType", "[UnitType] IN (1,2,3,4,5)");
                    table.ForeignKey(
                        name: "FK_Unit_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefaultReceipt = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                    table.UniqueConstraint("AK_Warehouse_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Warehouse_Type", "[Type] IN (1,2,3,4,5,6)");
                    table.ForeignKey(
                        name: "FK_Warehouse_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValue",
                schema: "extensibility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldValue", x => x.Id);
                    table.UniqueConstraint("AK_CustomFieldValue_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_CustomFieldValue_CustomFieldDefinition",
                        columns: x => new { x.CustomFieldDefinitionId, x.TenantId },
                        principalSchema: "extensibility",
                        principalTable: "CustomFieldDefinition",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomFieldValue_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.UniqueConstraint("AK_RolePermission_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role",
                        columns: x => new { x.RoleId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Role",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermission_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "binary(32)", fixedLength: true, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UploadedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    OcrStatus = table.Column<short>(type: "smallint", nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                    table.UniqueConstraint("AK_Attachment_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Attachment_OcrStatus", "[OcrStatus] IN (0,1,2,3)");
                    table.CheckConstraint("CK_Attachment_SizeBytes", "[SizeBytes] >= 0");
                    table.ForeignKey(
                        name: "FK_Attachment_TenantMembership",
                        columns: x => new { x.UploadedByMembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attachment_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipRole",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<short>(type: "smallint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipRole", x => x.Id);
                    table.UniqueConstraint("AK_MembershipRole_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_MembershipRole_ScopeType", "[ScopeType] IN (1,2)");
                    table.ForeignKey(
                        name: "FK_MembershipRole_Role",
                        columns: x => new { x.RoleId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Role",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipRole_TenantMembership",
                        columns: x => new { x.MembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipRole_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<short>(type: "smallint", nullable: false),
                    Severity = table.Column<short>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.UniqueConstraint("AK_Notification_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Notification_Severity", "[Severity] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_Notification_TenantMembership",
                        columns: x => new { x.MembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreference",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<short>(type: "smallint", nullable: false),
                    Channel = table.Column<short>(type: "smallint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreference", x => x.Id);
                    table.UniqueConstraint("AK_NotificationPreference_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_NotificationPreference_Channel", "[Channel] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_NotificationPreference_TenantMembership",
                        columns: x => new { x.MembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationPreference_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsBatchTracked = table.Column<bool>(type: "bit", nullable: false),
                    IsExpiryTracked = table.Column<bool>(type: "bit", nullable: false),
                    ShelfLifeDays = table.Column<int>(type: "int", nullable: true),
                    DefaultReasonCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.UniqueConstraint("AK_Product_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Product_ExpiryTracked", "[IsExpiryTracked] = 0 OR [ShelfLifeDays] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Product_BaseUnit",
                        columns: x => new { x.BaseUnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_Category",
                        columns: x => new { x.CategoryId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Category",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_DefaultReasonCode",
                        columns: x => new { x.DefaultReasonCodeId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "ReasonCode",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_PurchaseUnit",
                        columns: x => new { x.PurchaseUnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Device",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EnrollmentCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    IsSharedTerminal = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegisteredByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device", x => x.Id);
                    table.UniqueConstraint("AK_Device_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Device_Status", "[Status] IN (1,2,3)");
                    table.CheckConstraint("CK_Device_Type", "[Type] IN (1,2,3,4)");
                    table.ForeignKey(
                        name: "FK_Device_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Device_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    DurationDays = table.Column<short>(type: "smallint", nullable: false),
                    ExpectedAttendees = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    MealPlanNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                    table.UniqueConstraint("AK_Event_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Event_DurationDays", "[DurationDays] > 0");
                    table.CheckConstraint("CK_Event_EndAtUtc", "[EndAtUtc] > [StartAtUtc]");
                    table.CheckConstraint("CK_Event_ExpectedAttendees", "[ExpectedAttendees] >= 0");
                    table.CheckConstraint("CK_Event_Status", "[Status] IN (1,2,3,4,5)");
                    table.CheckConstraint("CK_Event_Type", "[Type] IN (1,2,3,4,5)");
                    table.ForeignKey(
                        name: "FK_Event_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Event_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipWarehouse",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanReceive = table.Column<bool>(type: "bit", nullable: false),
                    CanIssue = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipWarehouse", x => x.Id);
                    table.UniqueConstraint("AK_MembershipWarehouse_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_MembershipWarehouse_TenantMembership",
                        columns: x => new { x.MembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipWarehouse_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipWarehouse_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recipe",
                schema: "food",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MealTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YieldQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    YieldUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServingsPerYield = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    PreparationMinutes = table.Column<int>(type: "int", nullable: true),
                    CookingMinutes = table.Column<int>(type: "int", nullable: true),
                    Difficulty = table.Column<short>(type: "smallint", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipe", x => x.Id);
                    table.UniqueConstraint("AK_Recipe_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Recipe_YieldQuantity", "[YieldQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_Recipe_Attachment",
                        columns: x => new { x.ImageAttachmentId, x.TenantId },
                        principalSchema: "files",
                        principalTable: "Attachment",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipe_MealType",
                        columns: x => new { x.MealTypeId, x.TenantId },
                        principalSchema: "food",
                        principalTable: "MealType",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipe_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipe_Unit",
                        columns: x => new { x.YieldUnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipRoleWarehouse",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipRoleWarehouse", x => x.Id);
                    table.UniqueConstraint("AK_MembershipRoleWarehouse_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_MembershipRoleWarehouse_MembershipRole",
                        columns: x => new { x.MembershipRoleId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "MembershipRole",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembershipRoleWarehouse_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipRoleWarehouse_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Batch",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProductionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsQuarantined = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batch", x => x.Id);
                    table.UniqueConstraint("AK_Batch_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_Batch_Dates", "[ExpiryDate] IS NULL OR ([ProductionDate] IS NULL OR [ExpiryDate] >= [ProductionDate])");
                    table.ForeignKey(
                        name: "FK_Batch_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Batch_Supplier",
                        columns: x => new { x.SupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Batch_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductBarcode",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Symbology = table.Column<short>(type: "smallint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBarcode", x => x.Id);
                    table.UniqueConstraint("AK_ProductBarcode_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_ProductBarcode_Symbology", "[Symbology] IN (1,2,3,4,5,6)");
                    table.ForeignKey(
                        name: "FK_ProductBarcode_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductBarcode_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductPriceHistory",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPriceHistory", x => x.Id);
                    table.UniqueConstraint("AK_ProductPriceHistory_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_ProductPriceHistory_SourceType", "[SourceType] IN (1,2,3)");
                    table.CheckConstraint("CK_ProductPriceHistory_UnitPrice", "[UnitPrice] > 0");
                    table.ForeignKey(
                        name: "FK_ProductPriceHistory_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistory_Supplier",
                        columns: x => new { x.SupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistory_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistory_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistory_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductWarehouseSetting",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaximumQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReorderQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DefaultSupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShelfLifeDaysOverride = table.Column<int>(type: "int", nullable: true),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductWarehouseSetting", x => x.Id);
                    table.UniqueConstraint("AK_ProductWarehouseSetting_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_ProductWarehouseSetting_QuantityRange", "[MaximumQuantity] IS NULL OR [MinimumQuantity] IS NULL OR [MaximumQuantity] >= [MinimumQuantity]");
                    table.ForeignKey(
                        name: "FK_ProductWarehouseSetting_DefaultSupplier",
                        columns: x => new { x.DefaultSupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductWarehouseSetting_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductWarehouseSetting_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductWarehouseSetting_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBalance",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IncomingQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AverageUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LastMovementAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCountedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalance", x => x.Id);
                    table.UniqueConstraint("AK_StockBalance_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_StockBalance_OnHandQuantity", "[OnHandQuantity] >= 0");
                    table.CheckConstraint("CK_StockBalance_ReservedQuantity", "[ReservedQuantity] >= 0");
                    table.CheckConstraint("CK_StockBalance_ReservedWithinOnHand", "[ReservedQuantity] <= [OnHandQuantity]");
                    table.ForeignKey(
                        name: "FK_StockBalance_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalance_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalance_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierProduct",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastUnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LastPurchasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinimumOrderQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProduct", x => x.Id);
                    table.UniqueConstraint("AK_SupplierProduct_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_SupplierProduct_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProduct_Supplier",
                        columns: x => new { x.SupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProduct_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProduct_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversion",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitConversion", x => x.Id);
                    table.UniqueConstraint("AK_UnitConversion_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_UnitConversion_DistinctUnits", "[FromUnitId] <> [ToUnitId]");
                    table.CheckConstraint("CK_UnitConversion_Factor", "[Factor] > 0");
                    table.ForeignKey(
                        name: "FK_UnitConversion_FromUnit",
                        columns: x => new { x.FromUnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitConversion_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitConversion_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitConversion_ToUnit",
                        columns: x => new { x.ToUnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscription",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Auth = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscription", x => x.Id);
                    table.UniqueConstraint("AK_PushSubscription_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_PushSubscription_Device",
                        columns: x => new { x.DeviceId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Device",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PushSubscription_TenantMembership",
                        columns: x => new { x.MembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PushSubscription_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventAttendance",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsAttending = table.Column<bool>(type: "bit", nullable: false),
                    DaysAttending = table.Column<short>(type: "smallint", nullable: false),
                    MealsPerDay = table.Column<decimal>(type: "decimal(3,1)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendance", x => x.Id);
                    table.UniqueConstraint("AK_EventAttendance_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_EventAttendance_Attributable", "[MembershipId] IS NOT NULL OR [GuestName] IS NOT NULL");
                    table.CheckConstraint("CK_EventAttendance_DaysAttending", "[DaysAttending] > 0");
                    table.CheckConstraint("CK_EventAttendance_MealsPerDay", "[MealsPerDay] IS NULL OR [MealsPerDay] >= 0");
                    table.ForeignKey(
                        name: "FK_EventAttendance_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAttendance_TenantMembership",
                        columns: x => new { x.MembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAttendance_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventFoodCostSnapshot",
                schema: "food",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    MealTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalServings = table.Column<int>(type: "int", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostPerServing = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CostPerAttendee = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", fixedLength: true, nullable: false),
                    Breakdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFoodCostSnapshot", x => x.Id);
                    table.UniqueConstraint("AK_EventFoodCostSnapshot_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_EventFoodCostSnapshot_TotalCost", "[TotalCost] >= 0");
                    table.CheckConstraint("CK_EventFoodCostSnapshot_TotalServings", "[TotalServings] >= 0");
                    table.ForeignKey(
                        name: "FK_EventFoodCostSnapshot_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventFoodCostSnapshot_MealType",
                        columns: x => new { x.MealTypeId, x.TenantId },
                        principalSchema: "food",
                        principalTable: "MealType",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventFoodCostSnapshot_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventRequirement",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BasisType = table.Column<short>(type: "smallint", nullable: false),
                    QuantityPerBasisUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalRequiredQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRequirement", x => x.Id);
                    table.UniqueConstraint("AK_EventRequirement_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_EventRequirement_BasisType", "[BasisType] IN (1,2,3,4)");
                    table.CheckConstraint("CK_EventRequirement_QuantityPerBasisUnit", "[BasisType] = 1 OR [QuantityPerBasisUnit] IS NOT NULL");
                    table.CheckConstraint("CK_EventRequirement_TotalRequiredQuantity", "[TotalRequiredQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_EventRequirement_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRequirement_MealType",
                        columns: x => new { x.MealTypeId, x.TenantId },
                        principalSchema: "food",
                        principalTable: "MealType",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRequirement_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRequirement_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRequirement_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventShortageSnapshot",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IncomingQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RecommendedPurchaseQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventShortageSnapshot", x => x.Id);
                    table.UniqueConstraint("AK_EventShortageSnapshot_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_EventShortageSnapshot_RecommendedPurchaseQuantity", "[RecommendedPurchaseQuantity] >= 0");
                    table.CheckConstraint("CK_EventShortageSnapshot_RequiredQuantity", "[RequiredQuantity] >= 0");
                    table.CheckConstraint("CK_EventShortageSnapshot_ShortageQuantity", "[ShortageQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_EventShortageSnapshot_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventShortageSnapshot_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventShortageSnapshot_TenantMembership",
                        columns: x => new { x.CreatedByMembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventShortageSnapshot_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventShortageSnapshot_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventShortageSnapshot_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeCostSnapshot",
                schema: "food",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    YieldQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    YieldUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostPerYieldUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ServingsPerYield = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    CostPerServing = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CostSourceSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValuationMethod = table.Column<short>(type: "smallint", nullable: false),
                    LineCosts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeCostSnapshot", x => x.Id);
                    table.UniqueConstraint("AK_RecipeCostSnapshot_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_RecipeCostSnapshot_TotalCost", "[TotalCost] >= 0");
                    table.CheckConstraint("CK_RecipeCostSnapshot_ValuationMethod", "[ValuationMethod] IN (1,2,3)");
                    table.CheckConstraint("CK_RecipeCostSnapshot_YieldQuantity", "[YieldQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_RecipeCostSnapshot_Recipe",
                        columns: x => new { x.RecipeId, x.TenantId },
                        principalSchema: "food",
                        principalTable: "Recipe",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeCostSnapshot_TenantMembership",
                        columns: x => new { x.CreatedByMembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeCostSnapshot_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeCostSnapshot_Unit",
                        columns: x => new { x.YieldUnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeItem",
                schema: "food",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<short>(type: "smallint", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityPerYield = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WastagePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeItem", x => x.Id);
                    table.UniqueConstraint("AK_RecipeItem_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_RecipeItem_QuantityPerYield", "[QuantityPerYield] > 0");
                    table.CheckConstraint("CK_RecipeItem_WastagePercent", "[WastagePercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_RecipeItem_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeItem_Recipe",
                        columns: x => new { x.RecipeId, x.TenantId },
                        principalSchema: "food",
                        principalTable: "Recipe",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeItem_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeItem_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLot",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpirySortKey = table.Column<DateOnly>(type: "date", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FirstReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLot", x => x.Id);
                    table.UniqueConstraint("AK_InventoryLot_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_InventoryLot_OnHandQuantity", "[OnHandQuantity] >= 0");
                    table.CheckConstraint("CK_InventoryLot_ReservedQuantity", "[ReservedQuantity] >= 0");
                    table.CheckConstraint("CK_InventoryLot_ReservedWithinOnHand", "[ReservedQuantity] <= [OnHandQuantity]");
                    table.ForeignKey(
                        name: "FK_InventoryLot_Batch",
                        columns: x => new { x.BatchId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "Batch",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLot_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLot_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLot_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventConsumption",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventConsumption", x => x.Id);
                    table.UniqueConstraint("AK_EventConsumption_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_EventConsumption_ConsumedQuantity", "[ConsumedQuantity] >= 0");
                    table.CheckConstraint("CK_EventConsumption_PlannedQuantity", "[PlannedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_EventConsumption_Batch",
                        columns: x => new { x.BatchId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "Batch",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsumption_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsumption_MealType",
                        columns: x => new { x.MealTypeId, x.TenantId },
                        principalSchema: "food",
                        principalTable: "MealType",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsumption_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsumption_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsumption_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsumption_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipt",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedStockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PostedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipt", x => x.Id);
                    table.UniqueConstraint("AK_GoodsReceipt_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_GoodsReceipt_Status", "[Status] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Supplier",
                        columns: x => new { x.SupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLine",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineNumber = table.Column<short>(type: "smallint", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProductionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLine", x => x.Id);
                    table.UniqueConstraint("AK_GoodsReceiptLine_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_GoodsReceiptLine_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_GoodsReceiptLine_UnitCost", "[UnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_Batch",
                        columns: x => new { x.BatchId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "Batch",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_GoodsReceipt",
                        columns: x => new { x.GoodsReceiptId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipt",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseApproval",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<short>(type: "smallint", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StepNumber = table.Column<short>(type: "smallint", nullable: false),
                    ApproverMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<short>(type: "smallint", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiredPermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseApproval", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseApproval_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_PurchaseApproval_Decision", "[Decision] IN (1,2,3)");
                    table.CheckConstraint("CK_PurchaseApproval_EntityType", "[EntityType] IN (1,2)");
                    table.CheckConstraint("CK_PurchaseApproval_SingleTarget", "([EntityType] = 1 AND [PurchaseRequestId] IS NOT NULL AND [PurchaseOrderId] IS NULL) OR ([EntityType] = 2 AND [PurchaseRequestId] IS NULL AND [PurchaseOrderId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PurchaseApproval_TenantMembership",
                        columns: x => new { x.ApproverMembershipId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "TenantMembership",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseApproval_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrder",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", fixedLength: true, nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OrderedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AllowOverReceipt = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrder", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrder_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_PurchaseOrder_Status", "[Status] IN (1,2,3,4,5,6,7)");
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Supplier",
                        columns: x => new { x.SupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLine",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<short>(type: "smallint", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLine", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrderLine_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_PurchaseOrderLine_OrderedQuantity", "[OrderedQuantity] > 0");
                    table.CheckConstraint("CK_PurchaseOrderLine_ReceivedQuantity", "[ReceivedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_PurchaseOrder",
                        columns: x => new { x.PurchaseOrderId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrder",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequest",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiredByDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RequestedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConvertedPurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequest", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseRequest_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_PurchaseRequest_Priority", "[Priority] IN (1,2,3)");
                    table.CheckConstraint("CK_PurchaseRequest_Status", "[Status] IN (1,2,3,4,5,6)");
                    table.ForeignKey(
                        name: "FK_PurchaseRequest_PurchaseOrder",
                        columns: x => new { x.ConvertedPurchaseOrderId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrder",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequest_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequest_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequestLine",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<short>(type: "smallint", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PreferredSupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequestLine", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseRequestLine_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_PurchaseRequestLine_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLine_PreferredSupplier",
                        columns: x => new { x.PreferredSupplierId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "Supplier",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLine_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLine_PurchaseRequest",
                        columns: x => new { x.PurchaseRequestId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "PurchaseRequest",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLine_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLine_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCount",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ScopeType = table.Column<short>(type: "smallint", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CutoffAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CountedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCount", x => x.Id);
                    table.UniqueConstraint("AK_StockCount_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_StockCount_ScopeType", "[ScopeType] IN (1,2,3,4)");
                    table.CheckConstraint("CK_StockCount_Status", "[Status] IN (1,2,3,4,5)");
                    table.ForeignKey(
                        name: "FK_StockCount_Category",
                        columns: x => new { x.CategoryId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Category",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCount_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCount_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLine",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    VarianceQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CountedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<short>(type: "smallint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLine", x => x.Id);
                    table.UniqueConstraint("AK_StockCountLine_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_StockCountLine_Resolution", "[Resolution] IN (1,2,3,4)");
                    table.CheckConstraint("CK_StockCountLine_Variance", "[VarianceQuantity] IS NULL OR [VarianceQuantity] = [CountedQuantity] - [ExpectedQuantity]");
                    table.ForeignKey(
                        name: "FK_StockCountLine_Batch",
                        columns: x => new { x.BatchId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "Batch",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLine_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLine_StockCount",
                        columns: x => new { x.StockCountId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockCount",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLine_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLine_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockDocument",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MovementType = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    SourceGoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceStockCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceEventConsumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<short>(type: "smallint", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReversedByDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversesDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocument", x => x.Id);
                    table.UniqueConstraint("AK_StockDocument_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_StockDocument_MovementType", "[MovementType] IN (1,2,3,4,5,6,7)");
                    table.CheckConstraint("CK_StockDocument_PostedStatus", "[PostedAtUtc] IS NULL OR [Status] IN (2,4)");
                    table.CheckConstraint("CK_StockDocument_SingleSource", "([SourceType] = 0 AND [SourceGoodsReceiptId] IS NULL AND [SourceStockCountId] IS NULL AND [SourceEventConsumptionId] IS NULL) OR ([SourceType] = 1 AND [SourceGoodsReceiptId] IS NOT NULL AND [SourceStockCountId] IS NULL AND [SourceEventConsumptionId] IS NULL) OR ([SourceType] = 2 AND [SourceGoodsReceiptId] IS NULL AND [SourceStockCountId] IS NOT NULL AND [SourceEventConsumptionId] IS NULL) OR ([SourceType] = 3 AND [SourceGoodsReceiptId] IS NULL AND [SourceStockCountId] IS NULL AND [SourceEventConsumptionId] IS NOT NULL)");
                    table.CheckConstraint("CK_StockDocument_SourceType", "[SourceType] IN (0,1,2,3)");
                    table.CheckConstraint("CK_StockDocument_Status", "[Status] IN (1,2,3,4)");
                    table.CheckConstraint("CK_StockDocument_TransferPairing", "([MovementType] = 4 AND [CounterWarehouseId] IS NOT NULL) OR ([MovementType] <> 4 AND [CounterWarehouseId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_StockDocument_CounterWarehouse",
                        columns: x => new { x.CounterWarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_EventConsumption",
                        columns: x => new { x.SourceEventConsumptionId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "EventConsumption",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_GoodsReceipt",
                        columns: x => new { x.SourceGoodsReceiptId, x.TenantId },
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipt",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_ReasonCode",
                        columns: x => new { x.ReasonCodeId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "ReasonCode",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_ReversedBy",
                        columns: x => new { x.ReversedByDocumentId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockDocument",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_Reverses",
                        columns: x => new { x.ReversesDocumentId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockDocument",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_StockCount",
                        columns: x => new { x.SourceStockCountId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockCount",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocument_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockDocumentLine",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<short>(type: "smallint", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocumentLine", x => x.Id);
                    table.UniqueConstraint("AK_StockDocumentLine_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_StockDocumentLine_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_StockDocumentLine_Batch",
                        columns: x => new { x.BatchId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "Batch",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLine_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLine_StockDocument",
                        columns: x => new { x.StockDocumentId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockDocument",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLine_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLine_Unit",
                        columns: x => new { x.UnitId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockDocumentLine_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransaction",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockDocumentLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MovementType = table.Column<short>(type: "smallint", nullable: false),
                    Direction = table.Column<short>(type: "smallint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsReversal = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransaction", x => x.Id);
                    table.UniqueConstraint("AK_StockTransaction_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_StockTransaction_BalanceAfter", "[BalanceAfter] >= 0");
                    table.CheckConstraint("CK_StockTransaction_Direction", "[Direction] IN (1,2)");
                    table.CheckConstraint("CK_StockTransaction_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_StockTransaction_Batch",
                        columns: x => new { x.BatchId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "Batch",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_Event",
                        columns: x => new { x.EventId, x.TenantId },
                        principalSchema: "events",
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_Product",
                        columns: x => new { x.ProductId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "Product",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_ReasonCode",
                        columns: x => new { x.ReasonCodeId, x.TenantId },
                        principalSchema: "master",
                        principalTable: "ReasonCode",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_StockDocument",
                        columns: x => new { x.StockDocumentId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockDocument",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_StockDocumentLine",
                        columns: x => new { x.StockDocumentLineId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLine",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransaction_Warehouse",
                        columns: x => new { x.WarehouseId, x.TenantId },
                        principalSchema: "identity",
                        principalTable: "Warehouse",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_LockoutEndAtUtc",
                schema: "platform",
                table: "AppUser",
                column: "LockoutEndAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_AppUser_Email",
                schema: "platform",
                table: "AppUser",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AppUser_UserName",
                schema: "platform",
                table: "AppUser",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_TenantId_EntityName_EntityId",
                schema: "files",
                table: "Attachment",
                columns: new[] { "TenantId", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_UploadedByMembershipId_TenantId",
                schema: "files",
                table: "Attachment",
                columns: new[] { "UploadedByMembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_Attachment_StorageKey",
                schema: "files",
                table: "Attachment",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ActorUserId_OccurredAtUtc",
                schema: "audit",
                table: "AuditLog",
                columns: new[] { "ActorUserId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Category_OccurredAtUtc",
                schema: "audit",
                table: "AuditLog",
                columns: new[] { "Category", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CorrelationId",
                schema: "audit",
                table: "AuditLog",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityName_EntityId_OccurredAtUtc",
                schema: "audit",
                table: "AuditLog",
                columns: new[] { "EntityName", "EntityId", "OccurredAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TenantId_OccurredAtUtc",
                schema: "audit",
                table: "AuditLog",
                columns: new[] { "TenantId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSession_Device_Status",
                schema: "identity",
                table: "AuthSession",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSession_ExpiresAtUtc",
                schema: "identity",
                table: "AuthSession",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSession_Membership_Status",
                schema: "identity",
                table: "AuthSession",
                columns: new[] { "MembershipId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSession_Platform_Active",
                schema: "identity",
                table: "AuthSession",
                columns: new[] { "UserId", "Status" },
                filter: "[SessionScope] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Batch_ProductId_TenantId",
                schema: "inventory",
                table: "Batch",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Batch_SupplierId_TenantId",
                schema: "inventory",
                table: "Batch",
                columns: new[] { "SupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_Batch_TenantId_ProductId_BatchNumber_ExpiryDate",
                schema: "inventory",
                table: "Batch",
                columns: new[] { "TenantId", "ProductId", "BatchNumber", "ExpiryDate" },
                unique: true,
                filter: "[ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Batch_TenantId_ProductId_BatchNumber_NoExpiry",
                schema: "inventory",
                table: "Batch",
                columns: new[] { "TenantId", "ProductId", "BatchNumber" },
                unique: true,
                filter: "[ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentCategoryId_TenantId",
                schema: "master",
                table: "Category",
                columns: new[] { "ParentCategoryId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_Category_TenantId_Code",
                schema: "master",
                table: "Category",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Category_TenantId_ParentCategoryId_Code",
                schema: "master",
                table: "Category",
                columns: new[] { "TenantId", "ParentCategoryId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CustomFieldDefinition_TenantId_EntityName_FieldKey",
                schema: "extensibility",
                table: "CustomFieldDefinition",
                columns: new[] { "TenantId", "EntityName", "FieldKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValue_CustomFieldDefinitionId_TenantId",
                schema: "extensibility",
                table: "CustomFieldValue",
                columns: new[] { "CustomFieldDefinitionId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValue_TenantId",
                schema: "extensibility",
                table: "CustomFieldValue",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomFieldValue_CustomFieldDefinitionId_EntityName_EntityId",
                schema: "extensibility",
                table: "CustomFieldValue",
                columns: new[] { "CustomFieldDefinitionId", "EntityName", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Device_TenantId_WarehouseId_Status",
                schema: "identity",
                table: "Device",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Device_WarehouseId_TenantId",
                schema: "identity",
                table: "Device",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_Device_EnrollmentCode",
                schema: "identity",
                table: "Device",
                column: "EnrollmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Device_TenantId_DeviceCode",
                schema: "identity",
                table: "Device",
                columns: new[] { "TenantId", "DeviceCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_DocumentSequence_TenantId_DocumentType_PeriodKey",
                schema: "ops",
                table: "DocumentSequence",
                columns: new[] { "TenantId", "DocumentType", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_TenantId_Status_StartAtUtc",
                schema: "events",
                table: "Event",
                columns: new[] { "TenantId", "Status", "StartAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_WarehouseId_TenantId",
                schema: "events",
                table: "Event",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_Event_TenantId_Code",
                schema: "events",
                table: "Event",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendance_EventId_TenantId",
                schema: "events",
                table: "EventAttendance",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendance_MembershipId_TenantId",
                schema: "events",
                table: "EventAttendance",
                columns: new[] { "MembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_EventAttendance_TenantId_EventId_GuestName",
                schema: "events",
                table: "EventAttendance",
                columns: new[] { "TenantId", "EventId", "GuestName" },
                unique: true,
                filter: "[MembershipId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EventAttendance_TenantId_EventId_MembershipId",
                schema: "events",
                table: "EventAttendance",
                columns: new[] { "TenantId", "EventId", "MembershipId" },
                unique: true,
                filter: "[MembershipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_BatchId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_EventId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_MealTypeId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "MealTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_ProductId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_StockDocumentId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "StockDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_UnitId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumption_WarehouseId_TenantId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_EventConsumption_TenantId_EventId_ProductId_MealTypeId",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "TenantId", "EventId", "ProductId", "MealTypeId" },
                unique: true,
                filter: "[MealTypeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EventConsumption_TenantId_EventId_ProductId_NoMealType",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "TenantId", "EventId", "ProductId" },
                unique: true,
                filter: "[MealTypeId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventFoodCostSnapshot_EventId_TenantId",
                schema: "food",
                table: "EventFoodCostSnapshot",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventFoodCostSnapshot_MealTypeId_TenantId",
                schema: "food",
                table: "EventFoodCostSnapshot",
                columns: new[] { "MealTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_EventFoodCostSnapshot_TenantId_EventId_CalculatedAtUtc_MealTypeId",
                schema: "food",
                table: "EventFoodCostSnapshot",
                columns: new[] { "TenantId", "EventId", "CalculatedAtUtc", "MealTypeId" },
                unique: true,
                filter: "[MealTypeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EventFoodCostSnapshot_TenantId_EventId_CalculatedAtUtc_NoMealType",
                schema: "food",
                table: "EventFoodCostSnapshot",
                columns: new[] { "TenantId", "EventId", "CalculatedAtUtc" },
                unique: true,
                descending: new[] { false, false, true },
                filter: "[MealTypeId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventRequirement_EventId_TenantId",
                schema: "events",
                table: "EventRequirement",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRequirement_MealTypeId_TenantId",
                schema: "events",
                table: "EventRequirement",
                columns: new[] { "MealTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRequirement_ProductId_TenantId",
                schema: "events",
                table: "EventRequirement",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRequirement_UnitId_TenantId",
                schema: "events",
                table: "EventRequirement",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_EventRequirement_TenantId_EventId_ProductId_MealTypeId",
                schema: "events",
                table: "EventRequirement",
                columns: new[] { "TenantId", "EventId", "ProductId", "MealTypeId" },
                unique: true,
                filter: "[MealTypeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EventRequirement_TenantId_EventId_ProductId_NoMealType",
                schema: "events",
                table: "EventRequirement",
                columns: new[] { "TenantId", "EventId", "ProductId" },
                unique: true,
                filter: "[MealTypeId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventShortageSnapshot_CreatedByMembershipId_TenantId",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "CreatedByMembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventShortageSnapshot_EventId_TenantId",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventShortageSnapshot_ProductId_TenantId",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventShortageSnapshot_TenantId_EventId_ProductId",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "TenantId", "EventId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventShortageSnapshot_UnitId_TenantId",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventShortageSnapshot_WarehouseId_TenantId",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_EventShortageSnapshot_TenantId_EventId_CalculatedAtUtc",
                schema: "events",
                table: "EventShortageSnapshot",
                columns: new[] { "TenantId", "EventId", "CalculatedAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_PostedStockDocumentId_TenantId",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "PostedStockDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_PurchaseOrderId_TenantId",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "PurchaseOrderId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_SupplierId_TenantId",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "SupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_WarehouseId_ReceivedAtUtc",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "WarehouseId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_WarehouseId_TenantId",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_GoodsReceipt_TenantId_ReceiptNumber",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_BatchId_TenantId",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_GoodsReceiptId_TenantId",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "GoodsReceiptId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_ProductId_TenantId",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_PurchaseOrderLineId_TenantId",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "PurchaseOrderLineId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_UnitId_TenantId",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_GoodsReceiptLine_TenantId_GoodsReceiptId_LineNumber",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "TenantId", "GoodsReceiptId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLot_BatchId_TenantId",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLot_FEFO",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "TenantId", "WarehouseId", "ProductId", "ExpirySortKey", "FirstReceivedAtUtc", "Id" })
                .Annotation("SqlServer:Include", new[] { "OnHandQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLot_ProductId_TenantId",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLot_TenantId_ProductId",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLot_TenantId_WarehouseId_OnHandQuantity",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "TenantId", "WarehouseId", "OnHandQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLot_WarehouseId_TenantId",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryLot_Batch",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "TenantId", "WarehouseId", "BatchId" },
                unique: true,
                filter: "[BatchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryLot_NonBatched",
                schema: "inventory",
                table: "InventoryLot",
                columns: new[] { "TenantId", "WarehouseId", "ProductId" },
                unique: true,
                filter: "[BatchId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempt_EnrollmentCodeHash_OccurredAtUtc",
                schema: "identity",
                table: "LoginAttempt",
                columns: new[] { "EnrollmentCodeHash", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempt_IdentifierHash_OccurredAtUtc",
                schema: "identity",
                table: "LoginAttempt",
                columns: new[] { "IdentifierHash", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempt_IpAddress_OccurredAtUtc",
                schema: "identity",
                table: "LoginAttempt",
                columns: new[] { "IpAddress", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_MealType_TenantId_Code",
                schema: "food",
                table: "MealType",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRole_MembershipId_TenantId",
                schema: "identity",
                table: "MembershipRole",
                columns: new[] { "MembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRole_RoleId_TenantId",
                schema: "identity",
                table: "MembershipRole",
                columns: new[] { "RoleId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRole_TenantId",
                schema: "identity",
                table: "MembershipRole",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_MembershipRole_MembershipId_RoleId",
                schema: "identity",
                table: "MembershipRole",
                columns: new[] { "MembershipId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRoleWarehouse_MembershipRoleId_TenantId",
                schema: "identity",
                table: "MembershipRoleWarehouse",
                columns: new[] { "MembershipRoleId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRoleWarehouse_TenantId",
                schema: "identity",
                table: "MembershipRoleWarehouse",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRoleWarehouse_WarehouseId_TenantId",
                schema: "identity",
                table: "MembershipRoleWarehouse",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_MembershipRoleWarehouse_MembershipRoleId_WarehouseId",
                schema: "identity",
                table: "MembershipRoleWarehouse",
                columns: new[] { "MembershipRoleId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipWarehouse_MembershipId_TenantId",
                schema: "identity",
                table: "MembershipWarehouse",
                columns: new[] { "MembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipWarehouse_TenantId_WarehouseId",
                schema: "identity",
                table: "MembershipWarehouse",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipWarehouse_WarehouseId_TenantId",
                schema: "identity",
                table: "MembershipWarehouse",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_MembershipWarehouse_TenantId_MembershipId_WarehouseId",
                schema: "identity",
                table: "MembershipWarehouse",
                columns: new[] { "TenantId", "MembershipId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_MembershipId_TenantId",
                schema: "notifications",
                table: "Notification",
                columns: new[] { "MembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_TenantId_MembershipId_IsRead_CreatedAtUtc",
                schema: "notifications",
                table: "Notification",
                columns: new[] { "TenantId", "MembershipId", "IsRead", "CreatedAtUtc" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreference_MembershipId_TenantId",
                schema: "notifications",
                table: "NotificationPreference",
                columns: new[] { "MembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreference_TenantId",
                schema: "notifications",
                table: "NotificationPreference",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_NotificationPreference_MembershipId_Category_Channel",
                schema: "notifications",
                table: "NotificationPreference",
                columns: new[] { "MembershipId", "Category", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_Status_NextAttemptAtUtc",
                schema: "ops",
                table: "OutboxMessage",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_TenantId_CreatedAtUtc",
                schema: "ops",
                table: "OutboxMessage",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_TenantId",
                schema: "identity",
                table: "PasswordResetToken",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_UserId",
                schema: "identity",
                table: "PasswordResetToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_PasswordResetToken_TokenHash",
                schema: "identity",
                table: "PasswordResetToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Module",
                schema: "identity",
                table: "Permission",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "UX_Permission_Code",
                schema: "identity",
                table: "Permission",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PinCredential_TenantId_LockoutEndAtUtc",
                schema: "identity",
                table: "PinCredential",
                columns: new[] { "TenantId", "LockoutEndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_PinCredential_MembershipId",
                schema: "identity",
                table: "PinCredential",
                column: "MembershipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Plan_Code",
                schema: "platform",
                table: "Plan",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PlanFeature_PlanId_FeatureKey",
                schema: "platform",
                table: "PlanFeature",
                columns: new[] { "PlanId", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_BaseUnitId_TenantId",
                schema: "master",
                table: "Product",
                columns: new[] { "BaseUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId_TenantId",
                schema: "master",
                table: "Product",
                columns: new[] { "CategoryId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_DefaultReasonCodeId_TenantId",
                schema: "master",
                table: "Product",
                columns: new[] { "DefaultReasonCodeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_PurchaseUnitId_TenantId",
                schema: "master",
                table: "Product",
                columns: new[] { "PurchaseUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_TenantId_CategoryId_IsActive",
                schema: "master",
                table: "Product",
                columns: new[] { "TenantId", "CategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_TenantId_Name",
                schema: "master",
                table: "Product",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "UX_Product_TenantId_Sku",
                schema: "master",
                table: "Product",
                columns: new[] { "TenantId", "Sku" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcode_ProductId_TenantId",
                schema: "master",
                table: "ProductBarcode",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductBarcode_ProductId_Primary",
                schema: "master",
                table: "ProductBarcode",
                column: "ProductId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ProductBarcode_TenantId_Barcode",
                schema: "master",
                table: "ProductBarcode",
                columns: new[] { "TenantId", "Barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_ProductId_TenantId",
                schema: "purchasing",
                table: "ProductPriceHistory",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_SupplierId_TenantId",
                schema: "purchasing",
                table: "ProductPriceHistory",
                columns: new[] { "SupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_TenantId_ProductId_EffectiveFromUtc",
                schema: "purchasing",
                table: "ProductPriceHistory",
                columns: new[] { "TenantId", "ProductId", "EffectiveFromUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_UnitId_TenantId",
                schema: "purchasing",
                table: "ProductPriceHistory",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_WarehouseId_TenantId",
                schema: "purchasing",
                table: "ProductPriceHistory",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseSetting_DefaultSupplierId_TenantId",
                schema: "master",
                table: "ProductWarehouseSetting",
                columns: new[] { "DefaultSupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseSetting_ProductId_TenantId",
                schema: "master",
                table: "ProductWarehouseSetting",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseSetting_WarehouseId_TenantId",
                schema: "master",
                table: "ProductWarehouseSetting",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductWarehouseSetting_TenantId_WarehouseId_ProductId",
                schema: "master",
                table: "ProductWarehouseSetting",
                columns: new[] { "TenantId", "WarehouseId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseApproval_ApproverMembershipId_TenantId",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "ApproverMembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseApproval_PurchaseOrderId_TenantId",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "PurchaseOrderId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseApproval_PurchaseRequestId_TenantId",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "PurchaseRequestId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseApproval_PendingPerApprover",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "TenantId", "EntityType", "ApproverMembershipId" },
                unique: true,
                filter: "[Decision] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseApproval_TenantId_EntityType_StepNumber_ApproverMembershipId",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "TenantId", "EntityType", "StepNumber", "ApproverMembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_SourceRequestId_TenantId",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "SourceRequestId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_SupplierId_TenantId",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "SupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_TenantId_Status_ExpectedDeliveryDate",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "Status", "ExpectedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_TenantId_SupplierId_Status",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_WarehouseId_TenantId",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseOrder_TenantId_OrderNumber",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_ProductId_TenantId",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_PurchaseOrderId_TenantId",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                columns: new[] { "PurchaseOrderId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_TenantId_ProductId",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_UnitId_TenantId",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseOrderLine_TenantId_PurchaseOrderId_LineNumber",
                schema: "purchasing",
                table: "PurchaseOrderLine",
                columns: new[] { "TenantId", "PurchaseOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequest_ConvertedPurchaseOrderId_TenantId",
                schema: "purchasing",
                table: "PurchaseRequest",
                columns: new[] { "ConvertedPurchaseOrderId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequest_TenantId_Status",
                schema: "purchasing",
                table: "PurchaseRequest",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequest_WarehouseId_TenantId",
                schema: "purchasing",
                table: "PurchaseRequest",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseRequest_TenantId_RequestNumber",
                schema: "purchasing",
                table: "PurchaseRequest",
                columns: new[] { "TenantId", "RequestNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLine_PreferredSupplierId_TenantId",
                schema: "purchasing",
                table: "PurchaseRequestLine",
                columns: new[] { "PreferredSupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLine_ProductId_TenantId",
                schema: "purchasing",
                table: "PurchaseRequestLine",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLine_PurchaseRequestId_TenantId",
                schema: "purchasing",
                table: "PurchaseRequestLine",
                columns: new[] { "PurchaseRequestId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLine_TenantId",
                schema: "purchasing",
                table: "PurchaseRequestLine",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLine_UnitId_TenantId",
                schema: "purchasing",
                table: "PurchaseRequestLine",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseRequestLine_PurchaseRequestId_LineNumber",
                schema: "purchasing",
                table: "PurchaseRequestLine",
                columns: new[] { "PurchaseRequestId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscription_DeviceId_TenantId",
                schema: "notifications",
                table: "PushSubscription",
                columns: new[] { "DeviceId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscription_MembershipId_TenantId",
                schema: "notifications",
                table: "PushSubscription",
                columns: new[] { "MembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscription_TenantId",
                schema: "notifications",
                table: "PushSubscription",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_PushSubscription_Endpoint",
                schema: "notifications",
                table: "PushSubscription",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ReasonCode_TenantId_MovementType_Code",
                schema: "master",
                table: "ReasonCode",
                columns: new[] { "TenantId", "MovementType", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_ImageAttachmentId_TenantId",
                schema: "food",
                table: "Recipe",
                columns: new[] { "ImageAttachmentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_MealTypeId_TenantId",
                schema: "food",
                table: "Recipe",
                columns: new[] { "MealTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_YieldUnitId_TenantId",
                schema: "food",
                table: "Recipe",
                columns: new[] { "YieldUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_Recipe_TenantId_Code",
                schema: "food",
                table: "Recipe",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCostSnapshot_CreatedByMembershipId_TenantId",
                schema: "food",
                table: "RecipeCostSnapshot",
                columns: new[] { "CreatedByMembershipId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCostSnapshot_RecipeId_TenantId",
                schema: "food",
                table: "RecipeCostSnapshot",
                columns: new[] { "RecipeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCostSnapshot_TenantId_RecipeId_CalculatedAtUtc",
                schema: "food",
                table: "RecipeCostSnapshot",
                columns: new[] { "TenantId", "RecipeId", "CalculatedAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCostSnapshot_YieldUnitId_TenantId",
                schema: "food",
                table: "RecipeCostSnapshot",
                columns: new[] { "YieldUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItem_ProductId_TenantId",
                schema: "food",
                table: "RecipeItem",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItem_RecipeId_TenantId",
                schema: "food",
                table: "RecipeItem",
                columns: new[] { "RecipeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItem_TenantId",
                schema: "food",
                table: "RecipeItem",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItem_UnitId_TenantId",
                schema: "food",
                table: "RecipeItem",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_RecipeItem_RecipeId_LineNumber",
                schema: "food",
                table: "RecipeItem",
                columns: new[] { "RecipeId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_FamilyId_Status",
                schema: "identity",
                table: "RefreshToken",
                columns: new[] { "FamilyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TenantId",
                schema: "identity",
                table: "RefreshToken",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshToken_TokenHash",
                schema: "identity",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Role_TenantId_Code",
                schema: "identity",
                table: "Role",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                schema: "identity",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId_TenantId",
                schema: "identity",
                table: "RolePermission",
                columns: new[] { "RoleId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_TenantId_PermissionId",
                schema: "identity",
                table: "RolePermission",
                columns: new[] { "TenantId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "UX_RolePermission_RoleId_PermissionId",
                schema: "identity",
                table: "RolePermission",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_ProductId_TenantId",
                schema: "inventory",
                table: "StockBalance",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_TenantId_ProductId",
                schema: "inventory",
                table: "StockBalance",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_TenantId_WarehouseId_OnHandQuantity",
                schema: "inventory",
                table: "StockBalance",
                columns: new[] { "TenantId", "WarehouseId", "OnHandQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_WarehouseId_TenantId",
                schema: "inventory",
                table: "StockBalance",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockBalance",
                schema: "inventory",
                table: "StockBalance",
                columns: new[] { "TenantId", "WarehouseId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCount_CategoryId_TenantId",
                schema: "inventory",
                table: "StockCount",
                columns: new[] { "CategoryId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCount_PostedDocumentId_TenantId",
                schema: "inventory",
                table: "StockCount",
                columns: new[] { "PostedDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCount_WarehouseId_TenantId",
                schema: "inventory",
                table: "StockCount",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockCount_TenantId_WarehouseId_CountNumber",
                schema: "inventory",
                table: "StockCount",
                columns: new[] { "TenantId", "WarehouseId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLine_BatchId_TenantId",
                schema: "inventory",
                table: "StockCountLine",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLine_ProductId_TenantId",
                schema: "inventory",
                table: "StockCountLine",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLine_StockCountId_TenantId",
                schema: "inventory",
                table: "StockCountLine",
                columns: new[] { "StockCountId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLine_UnitId_TenantId",
                schema: "inventory",
                table: "StockCountLine",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockCountLine_TenantId_StockCountId_ProductId_BatchId",
                schema: "inventory",
                table: "StockCountLine",
                columns: new[] { "TenantId", "StockCountId", "ProductId", "BatchId" },
                unique: true,
                filter: "[BatchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_StockCountLine_TenantId_StockCountId_ProductId_NoBatch",
                schema: "inventory",
                table: "StockCountLine",
                columns: new[] { "TenantId", "StockCountId", "ProductId" },
                unique: true,
                filter: "[BatchId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_CounterWarehouseId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "CounterWarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_EventId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_ReasonCodeId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "ReasonCodeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_ReversedByDocumentId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "ReversedByDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_ReversesDocumentId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "ReversesDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_SourceEventConsumptionId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "SourceEventConsumptionId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_SourceGoodsReceiptId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "SourceGoodsReceiptId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_SourceStockCountId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "SourceStockCountId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_TenantId_EventId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_TenantId_MovementType_PostedAtUtc",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "MovementType", "PostedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_TenantId_SourceEventConsumptionId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "SourceEventConsumptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_TenantId_SourceGoodsReceiptId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "SourceGoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_TenantId_SourceStockCountId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "SourceStockCountId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_TenantId_WarehouseId_PostedAtUtc",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "WarehouseId", "PostedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocument_WarehouseId_TenantId",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockDocument_IdempotencyKey",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_StockDocument_TenantId_WarehouseId_DocumentNumber",
                schema: "inventory",
                table: "StockDocument",
                columns: new[] { "TenantId", "WarehouseId", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_BatchId_TenantId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_ProductId_TenantId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_StockDocumentId_TenantId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "StockDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_TenantId_BatchId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "TenantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_TenantId_ProductId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_UnitId_TenantId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLine_WarehouseId_TenantId",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_StockDocumentLine_TenantId_StockDocumentId_LineNumber",
                schema: "inventory",
                table: "StockDocumentLine",
                columns: new[] { "TenantId", "StockDocumentId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_BatchId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_EventId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "EventId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_ProductId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_ReasonCodeId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "ReasonCodeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockDocumentId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "StockDocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_StockDocumentLineId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "StockDocumentLineId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_TenantId_EventId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_TenantId_MovementType_PostedAtUtc",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "TenantId", "MovementType", "PostedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_TenantId_ProductId_PostedAtUtc",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "TenantId", "ProductId", "PostedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_TenantId_WarehouseId_PostedAtUtc",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "TenantId", "WarehouseId", "PostedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransaction_WarehouseId_TenantId",
                schema: "inventory",
                table: "StockTransaction",
                columns: new[] { "WarehouseId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EndAtUtc",
                schema: "platform",
                table: "Subscription",
                column: "EndAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_PlanId",
                schema: "platform",
                table: "Subscription",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_Tenant_Status",
                schema: "platform",
                table: "Subscription",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionUsage_TenantId_PeriodStartUtc_FeatureKey",
                schema: "platform",
                table: "SubscriptionUsage",
                columns: new[] { "TenantId", "PeriodStartUtc", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_TenantId_Name",
                schema: "purchasing",
                table: "Supplier",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "UX_Supplier_TenantId_Code",
                schema: "purchasing",
                table: "Supplier",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProduct_ProductId_TenantId",
                schema: "purchasing",
                table: "SupplierProduct",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProduct_SupplierId_TenantId",
                schema: "purchasing",
                table: "SupplierProduct",
                columns: new[] { "SupplierId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProduct_TenantId_ProductId",
                schema: "purchasing",
                table: "SupplierProduct",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProduct_UnitId_TenantId",
                schema: "purchasing",
                table: "SupplierProduct",
                columns: new[] { "UnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_SupplierProduct_TenantId_SupplierId_ProductId",
                schema: "purchasing",
                table: "SupplierProduct",
                columns: new[] { "TenantId", "SupplierId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Tenant_Code",
                schema: "identity",
                table: "Tenant",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TenantEntitlement_TenantId_FeatureKey",
                schema: "platform",
                table: "TenantEntitlement",
                columns: new[] { "TenantId", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembership_User",
                schema: "identity",
                table: "TenantMembership",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_TenantMembership_TenantId_UserId_Active",
                schema: "identity",
                table: "TenantMembership",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "UX_TenantSetting_TenantId",
                schema: "identity",
                table: "TenantSetting",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Unit_TenantId_Code",
                schema: "master",
                table: "Unit",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_FromUnitId_TenantId",
                schema: "master",
                table: "UnitConversion",
                columns: new[] { "FromUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_ProductId_TenantId",
                schema: "master",
                table: "UnitConversion",
                columns: new[] { "ProductId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_ToUnitId_TenantId",
                schema: "master",
                table: "UnitConversion",
                columns: new[] { "ToUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "UX_UnitConversion_Product",
                schema: "master",
                table: "UnitConversion",
                columns: new[] { "TenantId", "ProductId", "FromUnitId", "ToUnitId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_UnitConversion_TenantDefault",
                schema: "master",
                table: "UnitConversion",
                columns: new[] { "TenantId", "FromUnitId", "ToUnitId" },
                unique: true,
                filter: "[ProductId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Warehouse_TenantId_Code",
                schema: "identity",
                table: "Warehouse",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_EventConsumption_StockDocument",
                schema: "events",
                table: "EventConsumption",
                columns: new[] { "StockDocumentId", "TenantId" },
                principalSchema: "inventory",
                principalTable: "StockDocument",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_PurchaseOrder",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "PurchaseOrderId", "TenantId" },
                principalSchema: "purchasing",
                principalTable: "PurchaseOrder",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_StockDocument",
                schema: "purchasing",
                table: "GoodsReceipt",
                columns: new[] { "PostedStockDocumentId", "TenantId" },
                principalSchema: "inventory",
                principalTable: "StockDocument",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptLine_PurchaseOrderLine",
                schema: "purchasing",
                table: "GoodsReceiptLine",
                columns: new[] { "PurchaseOrderLineId", "TenantId" },
                principalSchema: "purchasing",
                principalTable: "PurchaseOrderLine",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseApproval_PurchaseOrder",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "PurchaseOrderId", "TenantId" },
                principalSchema: "purchasing",
                principalTable: "PurchaseOrder",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseApproval_PurchaseRequest",
                schema: "purchasing",
                table: "PurchaseApproval",
                columns: new[] { "PurchaseRequestId", "TenantId" },
                principalSchema: "purchasing",
                principalTable: "PurchaseRequest",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_PurchaseRequest",
                schema: "purchasing",
                table: "PurchaseOrder",
                columns: new[] { "SourceRequestId", "TenantId" },
                principalSchema: "purchasing",
                principalTable: "PurchaseRequest",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockCount_PostedDocument",
                schema: "inventory",
                table: "StockCount",
                columns: new[] { "PostedDocumentId", "TenantId" },
                principalSchema: "inventory",
                principalTable: "StockDocument",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batch_Tenant_TenantId",
                schema: "inventory",
                table: "Batch");

            migrationBuilder.DropForeignKey(
                name: "FK_Category_Tenant_TenantId",
                schema: "master",
                table: "Category");

            migrationBuilder.DropForeignKey(
                name: "FK_Event_Tenant_TenantId",
                schema: "events",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_Tenant_TenantId",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_Tenant_TenantId",
                schema: "purchasing",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_MealType_Tenant_TenantId",
                schema: "food",
                table: "MealType");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Tenant_TenantId",
                schema: "master",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_Tenant_TenantId",
                schema: "purchasing",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequest_Tenant_TenantId",
                schema: "purchasing",
                table: "PurchaseRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_ReasonCode_Tenant_TenantId",
                schema: "master",
                table: "ReasonCode");

            migrationBuilder.DropForeignKey(
                name: "FK_StockCount_Tenant_TenantId",
                schema: "inventory",
                table: "StockCount");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDocument_Tenant_TenantId",
                schema: "inventory",
                table: "StockDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Tenant_TenantId",
                schema: "purchasing",
                table: "Supplier");

            migrationBuilder.DropForeignKey(
                name: "FK_Unit_Tenant_TenantId",
                schema: "master",
                table: "Unit");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouse_Tenant_TenantId",
                schema: "identity",
                table: "Warehouse");

            migrationBuilder.DropForeignKey(
                name: "FK_Batch_Product",
                schema: "inventory",
                table: "Batch");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_Product",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_Batch_Supplier",
                schema: "inventory",
                table: "Batch");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_Supplier",
                schema: "purchasing",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_Supplier",
                schema: "purchasing",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_Event_Warehouse",
                schema: "events",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_Warehouse",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_Warehouse",
                schema: "purchasing",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_Warehouse",
                schema: "purchasing",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequest_Warehouse",
                schema: "purchasing",
                table: "PurchaseRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_StockCount_Warehouse",
                schema: "inventory",
                table: "StockCount");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDocument_CounterWarehouse",
                schema: "inventory",
                table: "StockDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDocument_Warehouse",
                schema: "inventory",
                table: "StockDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_Event",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDocument_Event",
                schema: "inventory",
                table: "StockDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_Batch",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_MealType",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_EventConsumption_StockDocument",
                schema: "events",
                table: "EventConsumption");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_StockDocument",
                schema: "purchasing",
                table: "GoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_StockCount_PostedDocument",
                schema: "inventory",
                table: "StockCount");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequest_PurchaseOrder",
                schema: "purchasing",
                table: "PurchaseRequest");

            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "AuthSession",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "CustomFieldValue",
                schema: "extensibility");

            migrationBuilder.DropTable(
                name: "DocumentSequence",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "EventAttendance",
                schema: "events");

            migrationBuilder.DropTable(
                name: "EventFoodCostSnapshot",
                schema: "food");

            migrationBuilder.DropTable(
                name: "EventRequirement",
                schema: "events");

            migrationBuilder.DropTable(
                name: "EventShortageSnapshot",
                schema: "events");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLine",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "InventoryLot",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "LoginAttempt",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "MembershipRoleWarehouse",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "MembershipWarehouse",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "NotificationPreference",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "PasswordResetToken",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "PinCredential",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "PlanFeature",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "ProductBarcode",
                schema: "master");

            migrationBuilder.DropTable(
                name: "ProductPriceHistory",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "ProductWarehouseSetting",
                schema: "master");

            migrationBuilder.DropTable(
                name: "PurchaseApproval",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseRequestLine",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PushSubscription",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "RecipeCostSnapshot",
                schema: "food");

            migrationBuilder.DropTable(
                name: "RecipeItem",
                schema: "food");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "RolePermission",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "StockBalance",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockCountLine",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockTransaction",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Subscription",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SubscriptionUsage",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SupplierProduct",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "TenantEntitlement",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantSetting",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UnitConversion",
                schema: "master");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinition",
                schema: "extensibility");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLine",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "MembershipRole",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Device",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Recipe",
                schema: "food");

            migrationBuilder.DropTable(
                name: "Permission",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "StockDocumentLine",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Plan",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Attachment",
                schema: "files");

            migrationBuilder.DropTable(
                name: "TenantMembership",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "AppUser",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Tenant",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Product",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Supplier",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "Warehouse",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Event",
                schema: "events");

            migrationBuilder.DropTable(
                name: "Batch",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "MealType",
                schema: "food");

            migrationBuilder.DropTable(
                name: "StockDocument",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "EventConsumption",
                schema: "events");

            migrationBuilder.DropTable(
                name: "GoodsReceipt",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "ReasonCode",
                schema: "master");

            migrationBuilder.DropTable(
                name: "StockCount",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Unit",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "master");

            migrationBuilder.DropTable(
                name: "PurchaseOrder",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseRequest",
                schema: "purchasing");
        }
    }
}
