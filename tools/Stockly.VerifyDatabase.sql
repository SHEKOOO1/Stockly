-- Stockly database verification.
--
-- Read-only. Classifies every one of the 66 domain tables and reports the numbers that
-- the isolation guarantees actually rest on, so a regression shows up as a changed number
-- rather than as a silently missing security object.
--
-- Usage:  sqlcmd -S localhost -d Stockly -E -i Stockly.VerifyDatabase.sql
--         or paste into SSMS against the Stockly database.
--
-- Expects the 13 project schemas and 66 domain tables created by:
--   20261001174239_InitialSchema       66 tables, 185 FKs, 96 CHECK constraints
--   20261001184551_TenantIsolation     security schema, RLS policy, 60 ownership CHECKs

SET NOCOUNT ON;

-- Classification of the domain tables.
--   tenant-owned : has a TenantId column, carries an RLS FILTER predicate AND a
--                  CK_<Table>_TenantScope CHECK constraint.
--   global       : no TenantId column at all, so it is deliberately outside tenant
--                  isolation. Platform reference data.
WITH tbl AS (
    SELECT t.object_id, t.name, SCHEMA_NAME(t.schema_id) AS schema_name
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.type = 'U'
      AND t.name <> '__EFMigrationsHistory'
),
scope_checked AS (
    SELECT k.parent_object_id
    FROM sys.check_constraints k
    WHERE k.name LIKE 'CK[_]%[_]TenantScope'
),
filtered AS (
    SELECT target_object_id FROM sys.security_predicates WHERE predicate_type = 0
),
classified AS (
    SELECT
        tbl.*,
        CASE
            WHEN tbl.object_id IN (SELECT parent_object_id FROM scope_checked) THEN 'tenant-owned'
            ELSE 'global'
        END AS kind,
        CASE WHEN EXISTS (
            SELECT 1 FROM sys.columns c
            WHERE c.object_id = tbl.object_id AND c.name = 'TenantId' AND c.is_nullable = 0
        ) THEN 0 ELSE 1 END AS tenant_id_nullable
    FROM tbl
)
SELECT
    (SELECT COUNT(*) FROM sys.schemas s
       JOIN sys.database_principals p ON p.principal_id = s.principal_id
      WHERE p.type IN ('U', 'S')
        AND s.name NOT IN ('guest', 'dbo', 'sys', 'INFORMATION_SCHEMA'))       AS project_schemas,

    (SELECT COUNT(*) FROM tbl)                                                 AS domain_tables,

    (SELECT COUNT(*) FROM classified WHERE kind = 'tenant-owned')               AS tenant_owned_tables,
    (SELECT COUNT(*) FROM classified WHERE kind = 'global')                    AS global_tables,
    (SELECT COUNT(*) FROM classified WHERE kind = 'tenant-owned'
                              AND tenant_id_nullable = 0)                       AS tenant_owned_notnull,
    (SELECT COUNT(*) FROM classified WHERE kind = 'tenant-owned'
                              AND tenant_id_nullable = 1)                       AS tenant_owned_nullable,

    (SELECT COUNT(*) FROM sys.foreign_keys)                                    AS foreign_keys,
    (SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled = 0)         AS check_constraints_total,
    (SELECT COUNT(*) FROM scope_checked)                                       AS tenant_scope_checks,
    (SELECT COUNT(*) FROM sys.check_constraints
      WHERE name LIKE 'CK[_]%[_]TenantScope' AND is_not_trusted = 1)            AS untrusted_tenant_scope,
    (SELECT COUNT(*) FROM sys.check_constraints
      WHERE name LIKE 'CK[_]%[_]TenantScope' AND is_disabled = 1)              AS disabled_tenant_scope,

    (SELECT COUNT(*) FROM filtered)                                            AS rls_filter_predicates,
    (SELECT COUNT(*) FROM sys.security_predicates WHERE predicate_type = 1)     AS rls_block_predicates,
    (SELECT COUNT(*) FROM classified WHERE kind = 'tenant-owned'
                              AND object_id NOT IN (SELECT * FROM filtered))     AS tenant_tables_missing_rls,
    (SELECT COUNT(*) FROM classified WHERE kind = 'global'
                              AND object_id IN (SELECT * FROM filtered))        AS global_tables_with_rls,
    (SELECT COUNT(*) FROM classified WHERE kind = 'global'
                              AND object_id IN (SELECT parent_object_id FROM scope_checked)) AS global_tables_with_scope_check,

    (SELECT COUNT(*) FROM sys.objects WHERE is_ms_shipped = 0
                                          AND type = 'SP')                      AS procedures,
    (SELECT COUNT(*) FROM sys.database_principals
      WHERE name = 'stockly_platform_role' AND type = 'R')                      AS platform_role;

-- Any row below is a defect, not information.
SELECT 'RLS GAP: tenant-owned table without a FILTER predicate' AS finding,
       SCHEMA_NAME(t.schema_id) + '.' + t.name AS object_name
FROM sys.tables t
WHERE t.is_ms_shipped = 0 AND t.type = 'U' AND t.name <> '__EFMigrationsHistory'
  AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = 'TenantId')
  AND NOT EXISTS (SELECT 1 FROM sys.security_predicates p
                   WHERE p.target_object_id = t.object_id AND p.predicate_type = 0)
UNION ALL
SELECT 'RLS GAP: tenant-owned table without an ownership CHECK constraint',
       SCHEMA_NAME(t.schema_id) + '.' + t.name
FROM sys.tables t
WHERE t.is_ms_shipped = 0 AND t.type = 'U' AND t.name <> '__EFMigrationsHistory'
  AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = 'TenantId')
  AND NOT EXISTS (SELECT 1 FROM sys.check_constraints k
                   WHERE k.parent_object_id = t.object_id AND k.name LIKE 'CK[_]%[_]TenantScope')
UNION ALL
SELECT 'UNTRUSTED ownership CHECK constraint (WITH NOCHECK)', cc.name
FROM sys.check_constraints cc
WHERE cc.name LIKE 'CK[_]%[_]TenantScope' AND cc.is_not_trusted = 1
UNION ALL
SELECT 'GLOBAL table unexpectedly carrying tenant isolation', SCHEMA_NAME(t.schema_id) + '.' + t.name
FROM sys.tables t
WHERE t.is_ms_shipped = 0 AND t.type = 'U' AND t.name <> '__EFMigrationsHistory'
  AND NOT EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = 'TenantId')
  AND EXISTS (SELECT 1 FROM sys.security_predicates p WHERE p.target_object_id = t.object_id)
ORDER BY finding, object_name;

-- The six global tables, for the record.
SELECT 'global table (outside tenant isolation by design)' AS kind,
       SCHEMA_NAME(t.schema_id) + '.' + t.name AS object_name
FROM sys.tables t
WHERE t.is_ms_shipped = 0 AND t.type = 'U' AND t.name <> '__EFMigrationsHistory'
  AND NOT EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = 'TenantId')
ORDER BY object_name;

SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;