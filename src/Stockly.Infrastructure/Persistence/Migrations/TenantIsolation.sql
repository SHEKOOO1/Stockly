-- ============================================================
-- Tenant isolation: SQL Server row-level security + tenant
-- ownership CHECK constraints (ADR-0051).
--
-- WHY BOTH MECHANISMS ARE REQUIRED
--
-- Verified against SQL Server 2025 (Express) before this was written:
--
--   * An RLS predicate MUST be an inline table-valued function declared
--     WITH SCHEMABINDING. A scalar UDF is rejected outright:
--     Msg 33270 "... is not an inline table-valued function".
--
--   * A FILTER predicate silently gates SELECT, UPDATE and DELETE --
--     a cross-tenant UPDATE and DELETE affect 0 rows even when EF query
--     filters are bypassed with IgnoreQueryFilters(). That is the whole
--     point of the mechanism.
--
--   * A FILTER predicate does NOT gate INSERT, and RLS cannot be made to
--     gate it on a regular table: ADD BLOCK PREDICATE is external-tables
--     only. A cross-tenant INSERT under an RLS policy alone was measured
--     succeeding, with the row persisting.
--
--   * TenantId reassignment is an UPDATE, and a FILTER predicate does
--     not stop a row moving from one tenant to another.
--
-- So INSERT and TenantId reassignment are enforced by a CHECK constraint
-- calling a scalar UDF, and SELECT/UPDATE/DELETE isolation is enforced by
-- RLS. Neither alone is sufficient. No triggers are used.
--
-- FAIL CLOSED
--
-- Every predicate returns 0 / raises a CHECK violation when SESSION_CONTEXT
-- holds no recognised scope. An unscoped connection sees nothing and can write
-- nothing. Platform authority is NOT granted by setting StocklyScope = 2:
-- the predicate also requires membership of stockly_platform_role, which
-- only ALTER ROLE can confer. Measured: a session that set scope 2 without
-- membership still read 0 rows.
--
-- SYNTAX NOTES FROM THIS ENGINE (SQL Server 2025)
--
--   * CREATE OR ALTER SECURITY POLICY is not accepted (Msg 102). Only
--     CREATE, and only as the sole statement in a batch.
--   * WITH (FILTER_PREDICATE = ON) is rejected as a syntax error here
--     (Msg 102, "Incorrect syntax near 'FILTER_PREDICATE'"). FILTER_PREDICATE
--     is ON by default, so the clause is omitted rather than worked around.
-- ============================================================

IF SCHEMA_ID(N'security') IS NULL
    EXEC(N'CREATE SCHEMA security');
GO

-- ------------------------------------------------------------
-- Role conveying platform authority. Granted separately by the
-- deployment principal; never to the ordinary application principal.
-- dbo cannot be granted a role (Msg 15405), so this is deliberately not
-- assumed to hold for the migration principal.
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'stockly_platform_role' AND type = 'R')
    CREATE ROLE stockly_platform_role AUTHORIZATION dbo;
GO

-- ------------------------------------------------------------
-- Scalar predicate for CHECK constraints: INSERT + TenantId reassignment.
-- ------------------------------------------------------------
CREATE OR ALTER FUNCTION security.fnStocklyRowTenantAllowed(@TenantId uniqueidentifier)
RETURNS bit
AS
BEGIN
    DECLARE @scope int = TRY_CONVERT(int, SESSION_CONTEXT(N'StocklyScope'));

    -- No scope at all: deny. Never "allow".
    IF @scope IS NULL
        RETURN 0;

    IF @scope = 1
    BEGIN
        DECLARE @tid uniqueidentifier = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'StocklyTenantId'));

        IF @tid IS NULL
            RETURN 0;

        -- @TenantId IS NOT NULL stops a tenant session from claiming a
        -- platform-level row. audit.AuditLog, ops.OutboxMessage and
        -- identity.AuthSession all carry a nullable TenantId for exactly that
        -- purpose, and a tenant must not be able to write one.
        RETURN CASE WHEN @TenantId IS NOT NULL AND @TenantId = @tid THEN 1 ELSE 0 END;
    END

    IF @scope = 2 AND IS_MEMBER(N'stockly_platform_role') = 1
        RETURN 1;

    RETURN 0;
END;
GO

-- ------------------------------------------------------------
-- Inline table-valued predicate for RLS: SELECT / UPDATE / DELETE.
--
-- WITH SCHEMABINDING is mandatory, and it is only possible because the body
-- reads SESSION_CONTEXT and IS_MEMBER and no table at all. Referencing a
-- table would make the function non-deterministic and SQL Server would
-- reject the policy.
--
-- TRY_CONVERT rather than CAST: a malformed or absent key must yield 0 rows,
-- not a conversion error that surfaces as a 500.
-- ------------------------------------------------------------
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
GO

-- ------------------------------------------------------------
-- One security policy covering every tenant-scoped table.
--
-- 60 tables carry TenantId. The 6 that do not are the genuinely global
-- tables and are deliberately left without a policy:
--   identity.LoginAttempt, identity.Permission, identity.Tenant,
--   platform.AppUser, platform.Plan, platform.PlanFeature
-- identity.Tenant is the tenant root itself and has no TenantId to filter on;
-- platform.AppUser holds the credential-adjacent columns the application must
-- resolve before any tenant is known.
--
-- One policy rather than 60: the tenant boundary is then a single named object
-- to review, audit or disable.
--
-- Discovered dynamically from sys.tables rather than from a hardcoded list so a
-- newly added tenant-scoped table cannot silently ship unprotected. sys.tables is
-- read inside sp_executesql, and CREATE SECURITY POLICY must be the only
-- statement in its batch, which is why the DDL is assembled first.
-- ------------------------------------------------------------
DECLARE @policy nvarchar(max) = N'';

SELECT @policy = @policy + N', ADD FILTER PREDICATE security.fnStocklyTenantVisible([TenantId]) ON '
    + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
FROM sys.tables t
WHERE EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId')
ORDER BY SCHEMA_NAME(t.schema_id), t.name;

IF @policy = N''
    THROW 51000, 'No tenant-scoped tables were found. The row-level security policy would be empty, which would leave the tenant boundary unenforced.', 1;

-- The leading comma from the FIRST clause becomes the batch-opening separator.
-- Assembled into one variable rather than concatenated at the EXEC: EXEC parses its
-- argument greedily and a following '+' is a syntax error there.
-- SUBSTRING(..., 3, ...) drops the leading ", " that the first clause contributes.
-- STUFF(@policy, 1, 2, N'') would drop the space too and weld the policy name onto
-- "ADD", producing "spStocklyTenantPolicyADD" -- a syntax error at every clause.
SET @policy = N'CREATE SECURITY POLICY security.spStocklyTenantPolicy '
    + SUBSTRING(@policy, 3, LEN(@policy))
    + N';';

EXEC sp_executesql @policy;
GO

PRINT 'RLS policy created';
GO

-- ------------------------------------------------------------
-- Tenant ownership CHECK constraints, one per tenant-scoped table.
--
-- WITH NOCHECK: existing rows cannot be validated at migration time. The
-- migration principal has no session scope and is not a member of
-- stockly_platform_role, so WITH CHECK would fail on every populated table. The
-- constraint is untrusted but fully enforced for every future INSERT and UPDATE,
-- which is what the ownership invariant needs. Re-validation under platform scope
-- is a separate, explicit step.
--
-- These do not overlap with RLS: RLS cannot gate INSERT at all, which is the gap
-- this closes.
-- ------------------------------------------------------------
DECLARE @checks nvarchar(max) = N'';

SELECT @checks = @checks + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' WITH NOCHECK ADD CONSTRAINT ' + QUOTENAME(N'CK_' + t.name + N'_TenantScope')
    + N' CHECK (security.fnStocklyRowTenantAllowed([TenantId]) = 1);' + CHAR(13) + CHAR(10)
FROM sys.tables t
WHERE EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId')
ORDER BY SCHEMA_NAME(t.schema_id), t.name;

IF @checks = N''
    THROW 51001, 'No tenant-scoped tables were found. No tenant ownership constraint would be created.', 1;

EXEC sp_executesql @checks;
GO

PRINT 'Tenant ownership CHECK constraints created';
GO