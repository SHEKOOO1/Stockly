using Microsoft.Extensions.Configuration;

namespace Stockly.Api.Configuration;

/// <summary>
/// Resolves the SQL Server connection string at startup.
/// <para>
/// <c>SECURITY_ARCHITECTURE.md</c> §7 requires the pepper and signing key to abort
/// boot when missing, and applies the same reasoning to the connection string: a
/// deployment that silently falls back to a default database is a deployment writing
/// production rows somewhere nobody chose. So absence is a hard failure at startup,
/// not a runtime error on the first request.
/// </para>
/// <para>
/// The value is treated as a secret throughout. It is never logged, never included
/// in an exception message, and never echoed. <c>AGENTS.md</c> §4 forbids committing
/// or logging a connection string, so this type has no branch that can emit one.
/// </para>
/// </summary>
public static class DatabaseConnection
{
    /// <summary>
    /// The configuration key, matching the committed <c>.env.example</c> template.
    /// The environment-variable form is <c>ConnectionStrings__StocklyDb</c>.
    /// </summary>
    public const string ConfigurationKey = "StocklyDb";

    /// <summary>
    /// The environment variable read as a fallback, for container deployments where
    /// the value comes from a secret store rather than a mounted file.
    /// </summary>
    public const string EnvironmentVariable = "ConnectionStrings__StocklyDb";

    /// <summary>
    /// Returns the connection string, or throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No usable value is configured. The message names the key and never the value.
    /// </exception>
    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var fromConfiguration = configuration.GetConnectionString(ConfigurationKey);
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
        {
            return fromConfiguration;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        throw new InvalidOperationException(
            "No database connection string is configured. Set ConnectionStrings:"
            + ConfigurationKey
            + " (see .env.example) or the "
            + EnvironmentVariable
            + " environment variable. The value is a secret and must never be committed.");
    }
}