using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Stockly.Api.Configuration;
using Xunit;

namespace Stockly.UnitTests.Configuration;

/// <summary>
/// <c>SECURITY_ARCHITECTURE.md</c> §7 and <c>AGENTS.md</c> §4 for the database
/// connection string: it is a secret, it is never committed, and its absence must
/// abort the boot rather than fall back to a default.
/// </para>
/// <para>
/// The fallback question is the one that matters. A default connection string means a
/// misconfigured deployment writes production rows into a database nobody chose, and
/// — because the default is readable in the source — an attacker reads it too. So
/// absence is a hard failure.
/// </para>
/// <para>
/// No SQL Server is required: <see cref="IConfiguration"/> is an in-memory dictionary.
/// Nothing here opens a connection, and nothing here may be satisfied by a real
/// server.
/// </para>
/// </summary>
public sealed class DatabaseConnectionTests
{
    /// <summary>A value that looks like a real secret, used only to prove it is not echoed.</summary>
    private const string SecretValue = "Server=db.internal;Database=Stockly;User Id=app;Password=hunter2";

    private static IConfiguration ConfigurationWith(params (string Key, string? Value)[] values)
    {
        var dictionary = values.ToDictionary(
            entry => entry.Key,
            entry => entry.Value,
            StringComparer.OrdinalIgnoreCase);

        return new ConfigurationBuilder().AddInMemoryCollection(dictionary).Build();
    }

    // ---- Resolution -----------------------------------------------------

    [Fact]
    public void A_configured_connection_string_is_returned()
    {
        DatabaseConnection
            .Resolve(ConfigurationWith(($"ConnectionStrings:{DatabaseConnection.ConfigurationKey}", SecretValue)))
            .Should().Be(SecretValue);
    }

    [Fact]
    public void The_key_matches_the_committed_env_example_template()
    {
        // If these diverge, an operator following the template configures a key the
        // application never reads, and the boot fails for a reason that looks like a
        // missing secret rather than a typo.
        DatabaseConnection.ConfigurationKey.Should().Be("StocklyDb");
        DatabaseConnection.EnvironmentVariable.Should().Be("ConnectionStrings__StocklyDb");
    }

    [Fact]
    public void A_null_configuration_is_refused()
    {
        var act = () => DatabaseConnection.Resolve(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---- Fail-safe on absence -------------------------------------------

    [Fact]
    public void A_missing_connection_string_aborts_the_boot()
    {
        var act = () => DatabaseConnection.Resolve(ConfigurationWith());

        act.Should().Throw<InvalidOperationException>(
            "a deployment that silently falls back to a default database writes rows "
            + "somewhere nobody chose");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_connection_string_aborts_the_boot(string value)
    {
        var act = () => DatabaseConnection.Resolve(
            ConfigurationWith(($"ConnectionStrings:{DatabaseConnection.ConfigurationKey}", value)));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void An_unrelated_connection_string_key_does_not_satisfy_the_check()
    {
        // Catching "SomeOtherDb" would silently point at the wrong database.
        var act = () => DatabaseConnection.Resolve(
            ConfigurationWith(("ConnectionStrings:SomeOtherDb", SecretValue)));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void There_is_no_default_value_anywhere_in_the_api_project()
    {
        // The real guard. If a default were reintroduced, the absence tests above
        // would keep passing while the behaviour regressed, so this asserts the
        // absence of a fallback in the source itself.
        var root = RepositoryRoot;
        var apiSources = Directory.GetFiles(
            Path.Combine(root, "src", "Stockly.Api"),
            "*.cs",
            SearchOption.AllDirectories);

        var offenders = apiSources
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => File.ReadAllLines(path).Select(line => (Path: path, Line: line)))
            .Where(entry => entry.Line.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase)
                || entry.Line.Contains("localdb", StringComparison.OrdinalIgnoreCase)
                || entry.Line.Contains("User Id=", StringComparison.OrdinalIgnoreCase))
            .Select(entry => Path.GetRelativePath(root, entry.Path))
            .Distinct()
            .ToArray();

        offenders.Should().BeEmpty(
            "no API source may carry a connection string; only Infrastructure's "
            + "documented design-time placeholder may, and it is never opened");
    }

    // ---- No secret leakage ----------------------------------------------

    [Fact]
    public void The_failure_message_never_contains_the_configured_value()
    {
        // Even in the misconfiguration where a value exists under the wrong key, the
        // message must not quote it.
        var act = () => DatabaseConnection.Resolve(
            ConfigurationWith(("ConnectionStrings:WrongKey", SecretValue)));

        var exception = act.Should().Throw<InvalidOperationException>().Which;

        exception.Message.Should().NotContain("hunter2");
        exception.Message.Should().NotContain("db.internal");
        exception.Message.Should().NotContain("User Id");
    }

    [Fact]
    public void The_failure_message_names_the_key_so_the_operator_can_fix_it()
    {
        var act = () => DatabaseConnection.Resolve(ConfigurationWith());

        var exception = act.Should().Throw<InvalidOperationException>().Which;

        exception.Message.Should().Contain(DatabaseConnection.ConfigurationKey);
        exception.Message.Should().Contain(DatabaseConnection.EnvironmentVariable);
        exception.Message.Should().Contain("secret");
    }

    [Fact]
    public void The_resolution_type_declares_no_property_or_field_that_could_carry_the_value()
    {
        // There is nowhere for the secret to be parked, which is a stronger guarantee
        // than remembering not to log it.
        var members = typeof(DatabaseConnection)
            .GetMembers(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Instance)
            .Where(member => member.MemberType is System.Reflection.MemberTypes.Field
                or System.Reflection.MemberTypes.Property)
            .Select(member => member.Name)
            .ToArray();

        members.Should().NotContain(name => name.Contains("Connection", StringComparison.OrdinalIgnoreCase)
            && !name.Contains("ConfigurationKey", StringComparison.Ordinal));
        members.Should().NotContain(name => name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        members.Should().NotContain(name => name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_only_constant_exposed_is_the_key_name()
    {
        // Guards against a "temporary" helper constant that becomes a default.
        typeof(DatabaseConnection)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => field.Name)
            .Should().BeEquivalentTo(
                [nameof(DatabaseConnection.ConfigurationKey), nameof(DatabaseConnection.EnvironmentVariable)]);
    }

    /// <summary>The repository root, resolved by walking up from the test assembly.</summary>
    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Stockly.slnx")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();
            return directory!.FullName;
        }
    }
}