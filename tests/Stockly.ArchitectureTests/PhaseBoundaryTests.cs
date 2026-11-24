using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Stockly.ArchitectureTests;

/// <summary>
/// Absence tests for the <strong>Phase 1 / Phase 2 boundary</strong>
/// (<c>PROJECT_BLUEPRINT.md</c> §25).
/// <para>
/// §25 was the subject of a real contradiction: <c>DEVELOPMENT_STATUS.md</c> §11.2
/// listed the 105-permission seed, the plan catalogue and the 11 tenant roles as
/// Phase 1 deliverables while §25 put all of them in Phase 2. It was resolved on
/// 2026-09-30 in favour of §25. A resolution recorded only in prose decays the
/// moment somebody reads the wrong document, so it is re-asserted here as a build
/// failure.
/// </para>
/// <para>
/// Each test below is a prohibition, and a prohibition cannot be verified by reading
/// a list of what exists — only by failing when the prohibited thing appears. These
/// tests deliberately FAIL if Phase 2 work is implemented early, because that is the
/// signal that the phase gate needs an owner decision rather than a silent merge.
/// </para>
/// <para>
/// They assert the <em>implementation</em> is absent. Entity <em>classes</em> for
/// <c>Permission</c>, <c>Role</c>, <c>RolePermission</c>, <c>Plan</c> and
/// <c>TenantEntitlement</c> must exist in Phase 1 — they are part of the 66-entity
/// schema — and their presence is explicitly not a violation. Seeding, deriving or
/// evaluating them is.
/// </para>
/// </summary>
public sealed class PhaseBoundaryTests
{
    private static readonly Assembly Domain = typeof(Stockly.Domain.Common.Entity).Assembly;
    private static readonly Assembly Application = typeof(Stockly.Application.Security.RouteScope).Assembly;
    private static readonly Assembly Infrastructure =
        typeof(Stockly.Infrastructure.Persistence.StocklyDbContext).Assembly;
    private static readonly Assembly Api = typeof(Stockly.Api.Common.SecurityHeadersMiddleware).Assembly;

    /// <summary>Every non-generated type across the four source projects.</summary>
    private static IReadOnlyList<Type> AllSourceTypes =>
    [
        .. Domain.GetTypes(),
        .. Application.GetTypes(),
        .. Infrastructure.GetTypes(),
        .. Api.GetTypes(),
    ];

    /// <summary>Types declared in source projects, excluding compiler-generated ones.</summary>
    private static IReadOnlyList<Type> HandWrittenTypes =>
        [.. AllSourceTypes.Where(type => !type.IsDefined(
            typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
            inherit: false))];

    /// <summary>
    /// Walks up from the test assembly to the repository root.
    /// <para>
    /// Resolved by walking rather than by counting `..` segments, because a wrong
    /// guess makes the assertion silently vacuous: a test that cannot find the file
    /// it is supposed to read reports "passed" while checking nothing.
    /// </para>
    /// </summary>
    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Stockly.slnx")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull("the test assembly lives under the repository root");
            return directory!.FullName;
        }
    }

    /// <summary>
    /// The API host source. Read as text because a runtime check would need a live
    /// host and a database, and the assertion must hold without either.
    /// </summary>
    private static string ApiProgramSource
    {
        get
        {
            var path = Path.Combine(RepositoryRoot, "src", "Stockly.Api", "Program.cs");
            File.Exists(path).Should().BeTrue("Program.cs exists at {0}", path);
            return File.ReadAllText(path);
        }
    }

    // ---- Seeding -------------------------------------------------------

    [Fact]
    public void No_seeder_type_exists()
    {
        // Covers PermissionSeeder, RoleSeeder, PlanSeeder, DbInitializer and every
        // name built on the same pattern.
        HandWrittenTypes
            .Where(type => type.Name.Contains("Seeder", StringComparison.OrdinalIgnoreCase)
                || type.Name.Contains("Initializer", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.FullName)
            .Should().BeEmpty("seeding the permission, role or plan catalogue is Phase 2 (§25)");
    }

    [Fact]
    public void No_model_configuration_declares_seed_data()
    {
        // `HasData` embeds rows in the model itself, which would put catalogue rows
        // into the migration and make Phase 2's seeding appear already done.
        var offenders = HandWrittenTypes
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.Name == "HasData")
            .Select(method => method.DeclaringType?.FullName)
            .Distinct()
            .ToArray();

        offenders.Should().BeEmpty("seeding is Phase 2; a HasData call would seed through the migration");
    }

    /// <summary>
    /// A migration line that loads rows: EF's <c>HasData</c>/<c>InsertData</c>, or a raw SQL
    /// statement that inserts. <c>MERGE</c> and <c>OPENROWSET</c> are not matched because the
    /// migrations do not use them and a speculative keyword list would only add noise.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex InsertPattern = new(
        @"\bInsertData\s*\(|\bHasData\s*\(|\bINSERT\s+(INTO\s+)?(\[|N'|dbo\.|master\.|identity\.|ops\.|audit\.|catalog\.|platform\.)",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    [Fact]
    public void The_initial_schema_migration_inserts_no_rows()
    {
        // A migration that INSERTed catalogue rows would be the most subtle form of
        // seeding: it looks like pure schema work.
        var migrations = Path.Combine(RepositoryRoot, "src", "Stockly.Infrastructure",
            "Persistence", "Migrations");

        Directory.Exists(migrations).Should().BeTrue("the migrations folder exists at {0}", migrations);

        var source = Directory.GetFiles(migrations, "*.cs", SearchOption.AllDirectories);
        source.Should().NotBeEmpty("InitialSchema must exist");

        var inserters = source
            .SelectMany(path => File.ReadAllText(path)
                .Split('\n')
                .Select(line => (Path: path, Line: line)))
            // Comment lines are excluded on purpose: the migrations explain in prose that they
            // are schema-only, and a comment mentioning an insert is not an insert.
            .Where(entry => !entry.Line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            // The rule is "no rows", so it matches an actual data-loading statement. It used to
            // flag every `migrationBuilder.Sql` call, which also fails DDL migrations that
            // legitimately need raw SQL -- the TenantIsolation migration creates the predicate
            // functions and the RLS policies that way, and is no more a seeder than a CHECK
            // constraint would be. `HasData` is matched by name, raw SQL by the statement it runs.
            .Where(entry => InsertPattern.IsMatch(entry.Line))
            .ToArray();

        inserters.Should().BeEmpty(
            "InitialSchema is schema-only; catalogue rows belong to a Phase 2 seeder, "
            + "not to a migration");
    }

    // ---- D1-D5 role derivation -----------------------------------------

    [Fact]
    public void No_role_derivation_service_exists()
    {
        var offenders = HandWrittenTypes
            .Where(type => type.Name.Contains("Derivation", StringComparison.OrdinalIgnoreCase)
                || type.Name.Contains("Derivator", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.FullName)
            .ToArray();

        offenders.Should().BeEmpty("the D1-D5 derivation is executed in Phase 2 (§25)");
    }

    // ---- RBAC ----------------------------------------------------------

    [Fact]
    public void No_permission_or_role_evaluation_service_exists()
    {
        // ADR-0034 scope separation answers "tenant or platform authority?" and is
        // Phase 1. Answering "may this principal perform X?" is RBAC and is Phase 2.
        var offenders = HandWrittenTypes
            .Where(type => type.Name.Contains("AuthorizationService", StringComparison.Ordinal)
                || type.Name.Contains("PermissionEvaluator", StringComparison.Ordinal)
                || type.Name.Contains("PermissionService", StringComparison.Ordinal)
                || type.Name.Contains("RoleResolver", StringComparison.Ordinal)
                || type.Name.Contains("EffectivePermissions", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        offenders.Should().BeEmpty("the full authorization chain is Phase 2 (§25)");
    }

    [Fact]
    public void The_scope_model_does_not_consult_permissions_or_roles()
    {
        // Guards against the scope model quietly growing into an RBAC evaluator. It
        // must remain a pure function of the route path and the `scp` claim.
        var scopeTypes = HandWrittenTypes
            .Where(type => type.Namespace?.StartsWith("Stockly.Application.Security", StringComparison.Ordinal) == true)
            .ToArray();

        scopeTypes.Should().NotBeEmpty();

        foreach (var type in scopeTypes)
        {
            var members = string.Join(
                ' ',
                type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.Name));

            members.Should().NotContain("Permission", "the scope model is not an RBAC evaluator");
            members.Should().NotContain("Role", "the scope model is not an RBAC evaluator");
        }
    }

    [Fact]
    public void The_tenant_context_offers_no_permission_or_role_api()
    {
        // The query filter's only inputs are tenant, membership and platform scope.
        var members = string.Join(
            ' ',
            typeof(Stockly.Infrastructure.Persistence.ITenantContext)
                .GetMembers()
                .Select(member => member.Name));

        members.Should().NotContain("Permission");
        members.Should().NotContain("Role");
        members.Should().NotContain("HasPermission");
    }

    [Fact]
    public void The_application_layer_never_references_the_identity_permission_entities()
    {
        // The entity classes are Phase 1 schema; *using* them to decide access is not.
        var offenders = Application.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(member => member.ToString() is not null
                    && member.ToString()!.Contains("Stockly.Domain.Modules.Identity", StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .ToArray();

        offenders.Should().BeEmpty(
            "Application must not consume identity entities; authorization decisions "
            + "belong to the API layer and RBAC to Phase 2");
    }

    // ---- Authentication ------------------------------------------------

    [Fact]
    public void No_token_issuing_or_hashing_code_exists()
    {
        var offenders = HandWrittenTypes
            .Where(type => type.Name.Contains("TokenService", StringComparison.Ordinal)
                || type.Name.Contains("TokenIssuer", StringComparison.Ordinal)
                || type.Name.Contains("JwtToken", StringComparison.Ordinal)
                || type.Name.Contains("PasswordHasher", StringComparison.Ordinal)
                || type.Name.Contains("PinHasher", StringComparison.Ordinal)
                || type.Name.Contains("RefreshTokenService", StringComparison.Ordinal)
                || type.Name.Contains("LockoutService", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        offenders.Should().BeEmpty("login, refresh rotation and lockout are Phase 2 (§25)");
    }

    [Fact]
    public void No_authentication_is_configured_in_the_api_host()
    {
        var text = ApiProgramSource;

        text.Should().NotContain("AddAuthentication", "authentication is Phase 2");
        text.Should().NotContain("AddJwtBearer", "token validation is Phase 2");
        text.Should().NotContain("UseAuthentication", "authentication is Phase 2");
    }

    [Fact]
    public void The_api_project_does_not_reference_the_jwt_bearer_package()
    {
        // A present-but-unused package reads as "JWT auth is wired up" when no
        // AddAuthentication call exists anywhere.
        var apiAssembly = typeof(Stockly.Api.Common.SecurityHeadersMiddleware).Assembly;
        var referenced = apiAssembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty);

        referenced.Should().NotContain(
            name => name.Contains("Authentication.JwtBearer", StringComparison.OrdinalIgnoreCase),
            "the JwtBearer package was removed for Phase 1 and returns in Phase 2");
    }

    // ---- Rate limiting --------------------------------------------------

    [Fact]
    public void No_rate_limiter_is_configured()
    {
        // Phase 1 owns only the caller-IP resolver that a Phase 2 limiter will key on.
        // Adding a limiter now would move a Phase 2 deliverable into Phase 1.
        var offenders = HandWrittenTypes
            .Where(type => type.Name.Contains("RateLimiter", StringComparison.Ordinal)
                || type.Name.Contains("Throttle", StringComparison.Ordinal)
                || type.Name.Contains("RateLimiting", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        offenders.Should().BeEmpty("rate limits are Phase 2 (§25)");
    }

    [Fact]
    public void No_rate_limiting_middleware_is_registered()
    {
        var text = ApiProgramSource;

        text.Should().NotContain("AddRateLimiter");
        text.Should().NotContain("UseRateLimiter");
    }

    // ---- The entities that legitimately DO exist in Phase 1 -------------

    [Fact]
    public void The_permission_role_and_plan_entities_exist_as_schema_only()
    {
        // The counter-test. Without it, the prohibition above could be satisfied by
        // deleting entities that the ratified 66-entity design requires.
        var required = new[]
        {
            "Stockly.Domain.Modules.Identity.Permission",
            "Stockly.Domain.Modules.Identity.Role",
            "Stockly.Domain.Modules.Identity.RolePermission",
            "Stockly.Domain.Modules.Platform.Plan",
            "Stockly.Domain.Modules.Platform.TenantEntitlement",
        };

        foreach (var name in required)
        {
            Domain.GetType(name).Should().NotBeNull(
                "{0} is one of the 66 ratified Phase 1 entities", name);
        }
    }
}