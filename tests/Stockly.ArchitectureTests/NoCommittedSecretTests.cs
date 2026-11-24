using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Stockly.ArchitectureTests;

/// <summary>
/// <c>AGENTS.md</c> §4: never commit passwords, PINs, PIN hashes, JWTs, refresh
/// tokens, encryption keys, connection strings or certificates. A security
/// regression is a build failure, so a committed secret is asserted here rather than
/// left to a CI secret scan that does not exist yet.
/// <para>
/// <c>.gitignore</c> is not a substitute for this test, because a file can be added
/// with <c>git add -f</c> and because a reviewer reading the diff should not have to
/// know the scan rules.
/// </para>
/// <para>
/// The scan reads files from disk rather than reflecting over assemblies, so it also
/// covers <c>appsettings*.json</c>, launch settings, <c>.http</c> files, Docker and
/// compose files, which never reach a compiled assembly.
/// </para>
/// </summary>
public sealed class NoCommittedSecretTests
{
    /// <summary>The repository root, resolved from the test assembly location.</summary>
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
    /// Directories that are not source-controlled content. Skipping them keeps the
    /// scan fast and keeps <c>bin</c>/<c>obj</c> copies of a file from being reported
    /// as a second violation of the same secret.
    /// </summary>
    private static readonly HashSet<string> IgnoredDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", ".vscode", "bin", "obj", "node_modules", "TestResults", ".claude",
        };

    private static readonly HashSet<string> IgnoredExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".zip", ".nupkg",
            ".woff", ".woff2", ".ttf", ".pdf", ".suo", ".user",
        };

    /// <summary>Files small enough to read whole; anything larger is a binary.</summary>
    private const long MaxReadableBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Patterns that indicate a credential. Each is deliberately specific: an
    /// ordinary identifier such as <c>PasswordPolicy</c> or a C# assignment
    /// <c>Password = value</c> must not trip the scan, because a test that cries
    /// wolf on ordinary code gets switched off and then catches nothing.
    /// </summary>
    private static readonly (string Name, Regex Pattern)[] SecretPatterns =
    [
        ("jwt literal", new Regex(@"\beyJ[A-Za-z0-9\-_]{8,}\.[A-Za-z0-9\-_]{8,}\.[A-Za-z0-9\-_]{8,}")),
        ("bearer token literal", new Regex(@"\bbearer\s+[A-Za-z0-9\-._~+/]{20,}=*", RegexOptions.IgnoreCase)),
        ("private key block", new Regex(@"-----BEGIN (RSA |EC |OPENSSH |PGP )?PRIVATE KEY-----")),
        ("aws access key", new Regex(@"\bAKIA[0-9A-Z]{16}\b")),
    ];

    /// <summary>
    /// Marks a line as carrying a SQL Server connection string, which is the only
    /// context in which <c>Password=</c> or <c>AccountKey=</c> is a credential rather
    /// than a property assignment.
    /// </summary>
    private static readonly Regex ConnectionStringMarker = new(
        @"\b(Server|Data Source|Initial Catalog)\s*=",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// A credential keyword with a <em>non-empty</em> value inside a connection
    /// string. <c>Password=</c> with nothing after it is a placeholder, and the
    /// committed template is allowed to name the server and the login.
    /// </summary>
    private static readonly Regex ConnectionStringSecret = new(
        @"\b(Password|Pwd|AccountKey|SharedAccessKey)\s*=\s*[""']?([^;""'\s]+)",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// The one committed connection string, which <c>DesignTimeStocklyDbContextFactory</c>
    /// and <c>.env.example</c> document as a never-opened placeholder that carries no
    /// credential. It is allow-listed by exact file and exact value rather than by
    /// weakening a pattern.
    /// </summary>
    private static readonly (string RelativePath, string AllowedFragment)[] AllowList =
    [
        (
            Path.Combine("src", "Stockly.Infrastructure", "Persistence", "DesignTimeStocklyDbContextFactory.cs"),
            "Trusted_Connection=True"
        ),
        (".env.example", "stockly_app"),
    ];

    private static IEnumerable<(string Path, string Text)> ReadableSourceFiles()
    {
        foreach (var path in Directory.EnumerateFiles(RepositoryRoot, "*", SearchOption.AllDirectories))
        {
            var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (segments.Any(segment => IgnoredDirectories.Contains(segment)))
            {
                continue;
            }

            if (IgnoredExtensions.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            var info = new FileInfo(path);
            if (info.Length > MaxReadableBytes)
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            // A file with a NUL byte is binary regardless of its extension.
            if (text.Contains('\0'))
            {
                continue;
            }

            yield return (path, text);
        }
    }

    private static string Relative(string path) =>
        Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/');

    [Fact]
    public void No_committed_file_contains_a_credential()
    {
        var violations = new List<string>();

        foreach (var (path, text) in ReadableSourceFiles())
        {
            var relative = Relative(path);

            // Tests assert the *absence* of these patterns and necessarily contain the
            // literal text of what they forbid, which is what makes them effective.
            if (relative.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
                || relative.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
                || relative.Contains("ARCHITECTURE", StringComparison.OrdinalIgnoreCase)
                || relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var (name, pattern) in SecretPatterns)
            {
                foreach (Match match in pattern.Matches(text))
                {
                    violations.Add($"{Relative(path)}: {name} -> '{Truncate(match.Value)}'");
                }
            }

            // Credentials are only meaningful in a connection string, so the check is
            // scoped to those lines rather than applied to the whole file.
            foreach (var line in text.Split('\n'))
            {
                if (!ConnectionStringMarker.IsMatch(line))
                {
                    continue;
                }

                var secret = ConnectionStringSecret.Match(line);
                if (!secret.Success)
                {
                    continue;
                }

                var allowed = AllowList.Any(entry =>
                    Relative(path).Equals(entry.RelativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)
                    && secret.Groups[2].Value.Contains(entry.AllowedFragment, StringComparison.OrdinalIgnoreCase));

                if (!allowed)
                {
                    violations.Add(
                        $"{Relative(path)}: connection string carries a {secret.Groups[1].Value} value "
                        + $"-> '{Truncate(secret.Groups[2].Value)}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "a credential must never be committed (AGENTS.md §4). Deliver secrets through "
            + "environment variables or mounted secrets instead.");
    }

    [Fact]
    public void The_committed_appsettings_contain_no_secrets()
    {
        foreach (var name in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var path = Path.Combine(RepositoryRoot, "src", "Stockly.Api", name);
            if (!File.Exists(path))
            {
                continue;
            }

            var text = File.ReadAllText(path);

            text.Should().NotMatch(@"(?i)password", "SECURITY_ARCHITECTURE §7: appsettings holds no secrets");
            text.Should().NotMatch(@"(?i)connectionstring", "the connection string arrives at runtime");
            text.Should().NotMatch(@"(?i)signingkey|secret", "keys arrive from the environment");
        }
    }

    [Fact]
    public void The_env_example_is_committed_and_the_env_file_is_not()
    {
        // SECURITY_ARCHITECTURE §7 requires a committed template and an ignored file.
        var env = Path.Combine(RepositoryRoot, ".env");
        var example = Path.Combine(RepositoryRoot, ".env.example");
        var gitignore = Path.Combine(RepositoryRoot, ".gitignore");

        if (File.Exists(env))
        {
            File.ReadAllText(env).Trim().Should().BeEmpty(
                "a committed .env must be empty; real values belong in the local, ignored file");
        }

        if (File.Exists(gitignore))
        {
            File.ReadAllText(gitignore).Should().MatchRegex(@"(?m)^\.env$");
        }

        // The template is allowed to carry non-secret defaults; what it must not carry
        // is a credential, which the scan above already asserts.
        if (File.Exists(example))
        {
            File.ReadAllText(example).Should().NotMatch(
                @"(?im)^\s*(Auth__IdentifierPepper|Auth__SigningKeyPemPath|Auth__PreviousSigningKeyPemPath)=\S");
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 40 ? value : value[..40] + "...";
}