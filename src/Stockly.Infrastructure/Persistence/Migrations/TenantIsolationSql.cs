namespace Stockly.Infrastructure.Persistence.Migrations;

/// <summary>
/// The raw SQL behind <see cref="20261001184551_TenantIsolation"/>.
/// <para>
/// Kept in its own file because it is not EF model operations. It is split into named
/// constants so the <c>Up</c>/<c>Down</c> migration reads as a list of intentions, and so
/// no batch boundary can be broken by accident: each constant is issued as exactly one
/// command, which is what lets <c>CREATE FUNCTION</c> and <c>CREATE SECURITY POLICY</c> —
/// both of which SQL Server requires to be alone in a batch — be part of a migration.
/// </para>
/// <para>
/// <strong>Batch rules encoded here</strong>, all verified against SQL Server 2025:
/// </para>
/// <list type="bullet">
/// <item><c>CREATE FUNCTION</c> must be the first statement in its batch.</item>
/// <item><c>CREATE SECURITY POLICY</c> must be the only statement in its batch. This engine
/// also rejects <c>CREATE OR ALTER SECURITY POLICY</c> (Msg 102), so the policy is dropped
/// first and recreated with plain <c>CREATE</c>.</item>
/// <item><c>WITH (FILTER_PREDICATE = ON)</c> is a syntax error on this engine (Msg 102) and
/// is omitted: FILTER_PREDICATE is the default.</item>
/// <item>A RLS predicate must be an inline table-valued function declared
/// <c>WITH SCHEMABINDING</c>. A scalar UDF is rejected outright (Msg 33270).</item>
/// </list>
/// <para>
/// <strong>Identifier handling.</strong> Table names are never concatenated into a string
/// literal by hand. Every identifier is passed through <c>QUOTENAME</c>, and each DDL
/// statement is executed separately via <c>sp_executesql</c> rather than being assembled
/// into one large batch. An earlier draft built a single multi-statement string; it was
/// fragile in a way that showed up as a syntax error at migration time, which is exactly
/// when a quoting mistake is most expensive.
/// </para>
/// </summary>
internal static class TenantIsolationSql
{
    /// <summary>
    /// The number of tables the ratified design says carry <c>TenantId</c>. Used in the
    /// <c>THROW</c> messages below so a mismatch names the expected figure rather than
    /// just "wrong number".
    /// </summary>
    internal const int ExpectedTenantScopedTableCount = 60;

    /// <summary>The <c>security</c> schema, created idempotently.</summary>
    internal static readonly string Schema = @"
IF SCHEMA_ID(N'security') IS NULL
    EXEC(N'CREATE SCHEMA security');
";

    /// <summary>
    /// The database role that confers platform authority.
    /// <para>
    /// Platform authority is a <em>role membership</em> and not merely a
    /// <c>SESSION_CONTEXT</c> value, so a session cannot grant itself the capability: only
    /// an <c>ALTER ROLE</c> executed against the database can. Verified: a tenant principal
    /// that set <c>StocklyScope = 2</c> still read zero rows, and
    /// <c>ALTER ROLE ... ADD MEMBER</c> as that principal failed with Msg 15151.
    /// </para>
    /// <para>
    /// Authorised by <c>dbo</c> rather than owned by it, because <c>dbo</c> cannot itself be
    /// granted a role (Msg 15405) and so cannot be a member of this one.
    /// </para>
    /// </summary>
    internal static readonly string Role = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'stockly_platform_role' AND type = 'R')
    CREATE ROLE stockly_platform_role AUTHORIZATION dbo;
";

    /// <summary>
    /// Scalar predicate backing the ownership CHECK constraints: the gate for
    /// <c>INSERT</c> and for <c>TenantId</c> reassignment.
    /// </summary>
    internal static readonly string OwnershipPredicateFunction = @"
CREATE OR ALTER FUNCTION security.fnStocklyRowTenantAllowed(@TenantId uniqueidentifier)
RETURNS bit
AS
BEGIN
    DECLARE @scope int = TRY_CONVERT(int, SESSION_CONTEXT(N'StocklyScope'));

    -- No recognised scope: deny. Never ""allow"".
    IF @scope IS NULL
        RETURN 0;

    IF @scope = 1
    BEGIN
        DECLARE @tid uniqueidentifier = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'StocklyTenantId'));

        IF @tid IS NULL
            RETURN 0;

        -- @TenantId IS NOT NULL stops a tenant session claiming a platform-level row.
        -- audit.AuditLog, ops.OutboxMessage and identity.AuthSession carry a nullable
        -- TenantId for exactly that purpose, and a tenant must not be able to write one.
        RETURN CASE WHEN @TenantId IS NOT NULL AND @TenantId = @tid THEN 1 ELSE 0 END;
    END

    IF @scope = 2 AND IS_MEMBER(N'stockly_platform_role') = 1
        RETURN 1;

    RETURN 0;
END;
";

    /// <summary>
    /// Inline table-valued predicate for the RLS policy: the gate for <c>SELECT</c>,
    /// <c>UPDATE</c> and <c>DELETE</c>.
    /// </summary>
    internal static readonly string VisibilityPredicateFunction = @"
CREATE OR ALTER FUNCTION security.fnStocklyTenantVisible(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT CAST(1 AS bit) AS Visible
    WHERE
        (
            TRY_CONVERT(int, SESSION_CONTEXT(N'StocklyScope')) = 1
            AND TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'StocklyTenantId')) IS NOT NULL
            AND @TenantId IS NOT NULL
            AND @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'StocklyTenantId'))
        )
        OR
        (
            TRY_CONVERT(int, SESSION_CONTEXT(N'StocklyScope')) = 2
            AND IS_MEMBER(N'stockly_platform_role') = 1
        )
);
";

    /// <summary>
    /// The single RLS policy covering every tenant-scoped table.
    /// <para>
    /// The 6 tables without <c>TenantId</c> are the genuinely global ones and are
    /// deliberately left without a policy: <c>identity.LoginAttempt</c>,
    /// <c>identity.Permission</c>, <c>identity.Tenant</c>, <c>platform.AppUser</c>,
    /// <c>platform.Plan</c>, <c>platform.PlanFeature</c>. <c>identity.Tenant</c> is the
    /// tenant root and has no <c>TenantId</c> to filter on; <c>platform.AppUser</c> holds
    /// the credential-adjacent columns the application must resolve before any tenant is
    /// known.
    /// </para>
    /// <para>
    /// Tables are discovered from <c>sys.tables</c> rather than hardcoded, so a newly added
    /// tenant-scoped table cannot silently ship unprotected. The expected count is asserted
    /// rather than trusted, so a change to the tenant-scoped set has to be an explicit,
    /// reviewed edit.
    /// </para>
    /// </summary>
    internal static readonly string Policy = $@"
SET NOCOUNT ON;

-- Re-application safety: drop any existing policy so CREATE below cannot collide.
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'spStocklyTenantPolicy')
    EXEC(N'DROP SECURITY POLICY security.spStocklyTenantPolicy');

DECLARE @tenantTables int = 0;

SELECT @tenantTables = COUNT(*)
FROM sys.tables t
WHERE t.is_ms_shipped = 0
  AND t.name <> N'__EFMigrationsHistory'
  AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId');

IF @tenantTables = 0
    THROW 51000, 'No tenant-scoped tables were found, so the row-level security policy would be empty and the tenant boundary would be unenforced.', 1;

IF @tenantTables <> {ExpectedTenantScopedTableCount}
    THROW 51003, 'Expected {ExpectedTenantScopedTableCount} tenant-scoped tables carrying TenantId, found a different number. The tenant-scoped set changed, so ADR-0051 and the isolation tests must be updated deliberately rather than by accident.', 1;

-- CREATE SECURITY POLICY must be the only statement in its batch, so the whole policy is
-- built first and handed to sp_executesql as one string. A cursor is used rather than
-- string concatenation of a clause list: QUOTENAME does the identifier quoting, and the
-- leading-comma / SUBSTRING trimming that a clause list needs disappears.
DECLARE @schemaName sysname, @tableName sysname;
DECLARE @clauses nvarchar(max) = N'';

DECLARE tenant_tables CURSOR LOCAL FAST_FORWARD FOR
    SELECT SCHEMA_NAME(t.schema_id), t.name
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name <> N'__EFMigrationsHistory'
      AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId')
    ORDER BY SCHEMA_NAME(t.schema_id), t.name;

OPEN tenant_tables;
FETCH NEXT FROM tenant_tables INTO @schemaName, @tableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Separator is added BEFORE each clause rather than after, and never trimmed off the
    -- end. An earlier draft appended the separator and removed it with
    -- SUBSTRING(@clauses, 1, LEN(@clauses) - 2); LEN() ignores trailing spaces, so that
    -- arithmetic cut two characters instead of the two-character separator, and silently
    -- removed the closing bracket of the final table name. Prefixing cannot miscount.
    IF LEN(@clauses) > 0
        SET @clauses = @clauses + N', ';

    SET @clauses = @clauses
        + N'ADD FILTER PREDICATE security.fnStocklyTenantVisible([TenantId]) ON '
        + QUOTENAME(@schemaName) + N'.' + QUOTENAME(@tableName);

    FETCH NEXT FROM tenant_tables INTO @schemaName, @tableName;
END

CLOSE tenant_tables;
DEALLOCATE tenant_tables;

IF @clauses IS NULL OR LEN(@clauses) = 0
    THROW 51000, 'The row-level security policy was built empty, so the tenant boundary would be unenforced.', 1;

DECLARE @policy nvarchar(max) =
    N'CREATE SECURITY POLICY security.spStocklyTenantPolicy ' + @clauses + N';';

EXEC sp_executesql @policy;
";

    /// <summary>
    /// One <c>CK_&lt;Table&gt;_TenantScope</c> CHECK constraint per tenant-scoped table.
    /// <para>
    /// <strong>Trust.</strong> An empty table validates under <c>WITH CHECK</c> and is marked
    /// trusted immediately. That is the case for a database built from scratch by
    /// <c>MigrateAsync()</c>, because the schema migration runs before any application data
    /// exists — verified: an empty table added <c>WITH CHECK</c> reports
    /// <c>is_not_trusted = 0</c>.
    /// </para>
    /// <para>
    /// A table that already holds rows is added <c>WITH NOCHECK</c>, because validating it
    /// requires platform authority that the migration principal does not hold (and
    /// <c>dbo</c> cannot be granted the role — Msg 15405). The constraint is still fully
    /// enforced for every future write; what it lacks is validation of pre-existing rows.
    /// Those are enumerated by <c>security.uspRevalidateTenantScopeConstraints</c>, and the
    /// warning below prints if any remain, so the gap is recorded rather than assumed away.
    /// </para>
    /// <para>
    /// These do not overlap with RLS. RLS cannot gate <c>INSERT</c> on a regular table at all,
    /// and a FILTER predicate does not stop a row moving between tenants.
    /// </para>
    /// </summary>
    internal static readonly string OwnershipConstraints = $@"
SET NOCOUNT ON;

DECLARE @schemaName sysname, @tableName sysname, @hasRows bit;
DECLARE @constraintName sysname, @parentObjectId int, @statement nvarchar(max);

DECLARE tenant_tables CURSOR LOCAL FAST_FORWARD FOR
    SELECT SCHEMA_NAME(t.schema_id),
           t.name,
           t.object_id,
           CASE WHEN EXISTS (SELECT 1 FROM sys.partitions p
                             WHERE p.object_id = t.object_id AND p.index_id IN (0, 1) AND p.rows > 0)
                THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name <> N'__EFMigrationsHistory'
      AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId')
    ORDER BY SCHEMA_NAME(t.schema_id), t.name;

OPEN tenant_tables;
FETCH NEXT FROM tenant_tables INTO @schemaName, @tableName, @parentObjectId, @hasRows;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @constraintName = N'CK_' + @tableName + N'_TenantScope';

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = @constraintName AND parent_object_id = @parentObjectId)
    BEGIN
        -- @hasRows drives WITH CHECK vs WITH NOCHECK, so one statement is correct both for
        -- a fresh database and for one that already holds rows.
        --
        -- Identifiers only ever reach the DDL through QUOTENAME. Catalog lookups compare
        -- against plain variables, so no table or constraint name is ever placed inside a
        -- string literal -- which is what makes this safe from quoting as well as injection.
        SET @statement =
            N'ALTER TABLE ' + QUOTENAME(@schemaName) + N'.' + QUOTENAME(@tableName)
            + CASE WHEN @hasRows = 1 THEN N' WITH NOCHECK' ELSE N' WITH CHECK' END
            + N' ADD CONSTRAINT ' + QUOTENAME(@constraintName)
            + N' CHECK (security.fnStocklyRowTenantAllowed([TenantId]) = 1);';

        EXEC sp_executesql @statement;
    END

    FETCH NEXT FROM tenant_tables INTO @schemaName, @tableName, @parentObjectId, @hasRows;
END

CLOSE tenant_tables;
DEALLOCATE tenant_tables;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name LIKE N'CK[_]%[_]TenantScope')
    THROW 51001, 'No tenant ownership constraints were created. No tenant-scoped table was found.', 1;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name LIKE N'CK[_]%[_]TenantScope' AND is_not_trusted = 1)
    PRINT 'WARNING: some TenantScope constraints are untrusted because their tables already held rows. Run security.uspRevalidateTenantScopeConstraints under platform authority.';
";

    /// <summary>
    /// Re-validation entry point.
    /// <para>
    /// A CHECK constraint is enforced for every future write as soon as it exists, trusted or
    /// not, so an untrusted constraint is not an isolation hole. What
    /// <c>is_not_trusted = 1</c> means is that the <em>existing</em> rows were never checked
    /// against the ownership invariant. This procedure closes that gap.
    /// </para>
    /// <para>
    /// It requires platform authority, because validating existing rows means evaluating the
    /// predicate over all of them, and a tenant-scoped evaluation would only see its own rows
    /// and so would prove nothing about the rest. The guard is a membership test on
    /// <c>stockly_platform_role</c>; it does not trust the session context on its own.
    /// </para>
    /// </summary>
    internal static readonly string RevalidationProcedure = @"
CREATE OR ALTER PROCEDURE security.uspRevalidateTenantScopeConstraints
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'stockly_platform_role') IS NULL OR IS_MEMBER(N'stockly_platform_role') <> 1
        THROW 51004, 'Revalidation requires membership of stockly_platform_role. A tenant-scoped validation would only evaluate the rows that tenant can already see and would prove nothing about the rest.', 1;

    IF TRY_CONVERT(int, SESSION_CONTEXT(N'StocklyScope')) <> 2
        THROW 51005, 'Revalidation requires platform scope in SESSION_CONTEXT, not a tenant or absent scope.', 1;

    DECLARE @schemaName sysname, @tableName sysname;
    DECLARE @statement nvarchar(max);

    DECLARE untrusted CURSOR LOCAL FAST_FORWARD FOR
        SELECT SCHEMA_NAME(t.schema_id), t.name
        FROM sys.check_constraints cc
        JOIN sys.tables t ON t.object_id = cc.parent_object_id
        WHERE cc.is_not_trusted = 1
          AND cc.name LIKE N'CK[_]%[_]TenantScope'
        ORDER BY SCHEMA_NAME(t.schema_id), t.name;

    OPEN untrusted;
    FETCH NEXT FROM untrusted INTO @schemaName, @tableName;

    IF @@FETCH_STATUS <> 0
    BEGIN
        CLOSE untrusted;
        DEALLOCATE untrusted;
        PRINT 'All TenantScope constraints are already trusted. Nothing to revalidate.';
        RETURN 0;
    END

    BEGIN TRANSACTION;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @statement =
            N'ALTER TABLE ' + QUOTENAME(@schemaName) + N'.' + QUOTENAME(@tableName)
            + N' WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(N'CK_' + @tableName + N'_TenantScope') + N';';

        EXEC sp_executesql @statement;

        FETCH NEXT FROM untrusted INTO @schemaName, @tableName;
    END

    CLOSE untrusted;
    DEALLOCATE untrusted;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE is_not_trusted = 1 AND name LIKE N'CK[_]%[_]TenantScope')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51006, 'Revalidation did not complete: at least one TenantScope constraint is still untrusted. Existing rows violate the tenant ownership invariant and must be investigated before trust is forced.', 1;
    END

    COMMIT TRANSACTION;

    PRINT 'Revalidation complete: every TenantScope constraint is now trusted.';
    RETURN 0;
END;
";

    /// <summary>Down: remove the ownership constraints.</summary>
    internal static readonly string DropOwnershipConstraints = @"
SET NOCOUNT ON;

DECLARE @schemaName sysname, @tableName sysname, @statement nvarchar(max);

DECLARE constrained CURSOR LOCAL FAST_FORWARD FOR
    SELECT SCHEMA_NAME(t.schema_id), t.name
    FROM sys.check_constraints cc
    JOIN sys.tables t ON t.object_id = cc.parent_object_id
    WHERE cc.name LIKE N'CK[_]%[_]TenantScope'
    ORDER BY SCHEMA_NAME(t.schema_id), t.name;

OPEN constrained;
FETCH NEXT FROM constrained INTO @schemaName, @tableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @statement =
        N'ALTER TABLE ' + QUOTENAME(@schemaName) + N'.' + QUOTENAME(@tableName)
        + N' DROP CONSTRAINT ' + QUOTENAME(N'CK_' + @tableName + N'_TenantScope') + N';';

    EXEC sp_executesql @statement;

    FETCH NEXT FROM constrained INTO @schemaName, @tableName;
END

CLOSE constrained;
DEALLOCATE constrained;
";

    /// <summary>Down: remove the RLS policy.</summary>
    internal static readonly string DropPolicy = @"
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'spStocklyTenantPolicy')
    EXEC(N'DROP SECURITY POLICY security.spStocklyTenantPolicy');
";

    /// <summary>Down: remove the re-validation procedure.</summary>
    internal static readonly string DropRevalidationProcedure = @"
IF OBJECT_ID(N'security.uspRevalidateTenantScopeConstraints', N'P') IS NOT NULL
    EXEC(N'DROP PROCEDURE security.uspRevalidateTenantScopeConstraints');
";

    /// <summary>Down: remove the two predicate functions.</summary>
    internal static readonly string DropPredicateFunctions = @"
IF OBJECT_ID(N'security.fnStocklyRowTenantAllowed', N'FN') IS NOT NULL
    EXEC(N'DROP FUNCTION security.fnStocklyRowTenantAllowed');

IF OBJECT_ID(N'security.fnStocklyTenantVisible', N'IF') IS NOT NULL
    EXEC(N'DROP FUNCTION security.fnStocklyTenantVisible');
";

    /// <summary>
    /// Down: remove the platform role, clearing members first because a role that still has
    /// members cannot be dropped.
    /// </summary>
    internal static readonly string DropRole = @"
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'stockly_platform_role' AND type = 'R')
BEGIN
    DECLARE @member sysname, @statement nvarchar(max);

    DECLARE members CURSOR LOCAL FAST_FORWARD FOR
        SELECT USER_NAME(member_principal_id) FROM sys.database_role_members
        WHERE role_principal_id = DATABASE_PRINCIPAL_ID(N'stockly_platform_role');

    OPEN members;
    FETCH NEXT FROM members INTO @member;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @statement = N'ALTER ROLE stockly_platform_role DROP MEMBER ' + QUOTENAME(@member) + N';';
        EXEC sp_executesql @statement;
        FETCH NEXT FROM members INTO @member;
    END

    CLOSE members;
    DEALLOCATE members;

    EXEC(N'DROP ROLE stockly_platform_role');
END;
";

    /// <summary>
    /// Down: remove the schema, but only once it is empty. SQL Server refuses to drop a
    /// schema that still contains objects, and swallowing that would leave a half-torn-down
    /// database whose isolation objects are gone but whose schema remains — a state that
    /// looks tidy and is not.
    /// </summary>
    internal static readonly string DropSchema = @"
IF SCHEMA_ID(N'security') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.objects o
        WHERE o.schema_id = SCHEMA_ID(N'security') AND o.type IN ('U', 'F', 'IF', 'TF', 'P', 'V'))
        THROW 51007, 'The security schema still contains objects and was not dropped. Down() removes the isolation objects it knows about; anything else must be removed deliberately.', 1;

    EXEC(N'DROP SCHEMA security');
END;
";
}