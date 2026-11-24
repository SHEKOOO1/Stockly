using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stockly.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Tenant isolation (ADR-0051): SQL Server row-level security plus per-table tenant
    /// ownership CHECK constraints.
    /// <para>
    /// Scaffolded by <c>dotnet ef migrations add</c> so the designer file and the model
    /// snapshot stay authoritative and hand-editable, then given real contents here. The
    /// scaffolded <c>Up</c>/<c>Down</c> were empty because none of these objects are part of
    /// the EF model -- which is the point. RLS policies and <c>SESSION_CONTEXT</c> predicates
    /// are engine features EF cannot represent, and their absence from the model is what
    /// stops a later <c>migrations add</c> from trying to drop and recreate the security
    /// boundary as if it were a column default. The model did not change, so the snapshot
    /// did not change, and the diff above is only the class declaration.
    /// </para>
    /// <para>
    /// Applied by <c>Database.MigrateAsync()</c> like any other migration. There is no
    /// external <c>sqlcmd</c> step, so the test path and the deployment path are the same
    /// path.
    /// </para>
    /// </summary>
    public partial class TenantIsolation : Migration
    {
        /// <summary>
        /// How many tables are expected to carry <c>TenantId</c>, asserted inside the
        /// migration rather than assumed. A tenant-scoped table added without isolation
        /// must break the build, not ship unprotected.
        /// </summary>
        private const int ExpectedTenantScopedTableCount = 60;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One migrationBuilder.Sql call per statement, deliberately.
            //
            // SQL Server requires CREATE FUNCTION to be the first statement of its batch and
            // CREATE SECURITY POLICY to be the only statement of its batch. A migration
            // batch is one command, so these cannot be concatenated into a single call.
            // This is not stylistic: joining them fails with Msg 111 and Msg 102.

            migrationBuilder.Sql(TenantIsolationSql.Schema);

            migrationBuilder.Sql(TenantIsolationSql.Role);

            migrationBuilder.Sql(TenantIsolationSql.OwnershipPredicateFunction);

            migrationBuilder.Sql(TenantIsolationSql.VisibilityPredicateFunction);

            migrationBuilder.Sql(TenantIsolationSql.Policy);

            migrationBuilder.Sql(TenantIsolationSql.OwnershipConstraints);

            migrationBuilder.Sql(TenantIsolationSql.RevalidationProcedure);

            migrationBuilder.Sql(AssertTenantScopedTableCount(ExpectedTenantScopedTableCount));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Order is forced by dependency: the constraints and the policy call the
            // functions, so they must be gone before the functions are.

            migrationBuilder.Sql(TenantIsolationSql.DropOwnershipConstraints);

            migrationBuilder.Sql(TenantIsolationSql.DropPolicy);

            migrationBuilder.Sql(TenantIsolationSql.DropRevalidationProcedure);

            migrationBuilder.Sql(TenantIsolationSql.DropPredicateFunctions);

            migrationBuilder.Sql(TenantIsolationSql.DropRole);

            migrationBuilder.Sql(TenantIsolationSql.DropSchema);
        }

        /// <summary>
        /// Verifies the migration produced the isolation the ratified design specifies, in
        /// the same transaction as the migration itself.
        /// <para>
        /// Putting the check here rather than in a test means a mis-provisioned database
        /// fails at deploy time, in the same transaction that created it, instead of
        /// surfacing later as a runtime authorisation error. The test suite asserts the same
        /// numbers independently; this is the deploy-time guard, not a substitute for it.
        /// </para>
        /// </summary>
        private static string AssertTenantScopedTableCount(int expected) => $@"
SET NOCOUNT ON;

IF (SELECT COUNT(*) FROM sys.security_predicates) <> {expected}
    THROW 51010, 'Expected {expected} row-level security filter predicates, found a different number. The tenant boundary is not what ADR-0051 specifies.', 1;

IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name LIKE N'CK[_]%[_]TenantScope') <> {expected}
    THROW 51011, 'Expected {expected} tenant ownership CHECK constraints, found a different number.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name <> N'__EFMigrationsHistory'
      AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId')
      AND NOT EXISTS (SELECT 1 FROM sys.check_constraints cc WHERE cc.name = N'CK_' + t.name + N'_TenantScope'))
    THROW 51012, 'At least one tenant-scoped table has no tenant ownership CHECK constraint. This is an isolation gap and must not be deployed.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name <> N'__EFMigrationsHistory'
      AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId')
      AND NOT EXISTS (
          SELECT 1 FROM sys.security_predicates sp
          WHERE sp.target_object_id = t.object_id
            AND sp.predicate_type = 0
            AND sp.predicate_definition LIKE N'%fnStocklyTenantVisible%'))
    THROW 51013, 'At least one tenant-scoped table has no row-level security filter predicate. This is an isolation gap and must not be deployed.', 1;
";
    }
}