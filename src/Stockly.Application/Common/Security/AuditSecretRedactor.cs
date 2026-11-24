using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stockly.Application.Common.Security;

/// <summary>
/// Refuses audit payloads that contain a secret, by <em>name</em> and by
/// <em>shape</em>, before serialisation.
/// <para>
/// <c>AGENTS.md</c> §4 and <c>SECURITY_ARCHITECTURE.md</c> §13 forbid logging,
/// returning, seeding or committing passwords, PINs, PIN hashes, JWTs, refresh
/// tokens, encryption keys or connection strings. Relying on a reviewer to notice
/// is not a control, so the writer refuses rather than redacts: a redacted field
/// is still a signal that the field existed, and a partially-redacted payload is
/// easy to get wrong silently.
/// </para>
/// <para>
/// Refusal is deliberate. A caller that trips it has a bug in its payload
/// selection, and that bug should surface as a failed request with an audit row
/// recording the refusal — not as silently lost audit data.
/// </para>
/// </summary>
public static partial class AuditSecretRedactor
{
    /// <summary>
    /// Whole property-name tokens that are secret on their own. Matching whole
    /// tokens rather than substrings keeps ordinary words that merely contain a
    /// secret word out of the net: <c>shipping</c> contains <c>pin</c> and must not
    /// be flagged.
    /// </summary>
    private static readonly HashSet<string> ForbiddenTokens = new(StringComparer.Ordinal)
    {
        "password", "passwd", "pwd", "passphrase",
        "pin",
        "secret",
        "jwt", "bearer",
        "salt", "pepper",
        "credential", "credentials",
    };

    /// <summary>
    /// Compound names that do not tokenise (all-lowercase single words) but are
    /// unambiguous. Checked as substrings of the separator-stripped name, which
    /// is safe because each entry is specific.
    /// </summary>
    private static readonly string[] ForbiddenCompounds =
    [
        "pinhash",
        "passwordhash",
        "tokenhash",
        "refreshtoken",
        "accesstoken",
        "idtoken",
        "authtoken",
        "connectionstring",
        "connstr",
        "privatekey",
        "encryptionkey",
        "signingkey",
        "apikey",
        "secretkey",
    ];

    /// <summary>
    /// Adjacent token pairs that are secret only in combination, e.g. a bare
    /// <c>token</c> is not automatically a secret but a <c>token</c> next to a
    /// <c>hash</c> is.
    /// </summary>
    private static readonly HashSet<string> ForbiddenTokenPairs = new(StringComparer.Ordinal)
    {
        "token+hash", "refresh+token", "access+token", "id+token",
        "refresh+hash", "pin+hash", "password+hash",
        "private+key", "encryption+key", "signing+key", "api+key", "secret+key",
        "connection+string", "conn+str",
    };

    /// <summary>
    /// Header-style names that are secret regardless of tokenisation.
    /// </summary>
    private static readonly HashSet<string> AlwaysSecretNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie",
    };

    [GeneratedRegex(
        @"\beyJ[A-Za-z0-9_\-]{4,}\.[A-Za-z0-9_\-]{4,}\.[A-Za-z0-9_\-]*",
        RegexOptions.CultureInvariant)]
    private static partial Regex JwtLikeTokenRegex();

    [GeneratedRegex(
        @"(?i)\b(server|data source|initial catalog|user id|uid|password|pwd)\s*=\s*[^;\s""']+",
        RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringFragmentRegex();

    /// <summary>
    /// True when a property name must never be written to an audit payload.
    /// Naming style cannot hide a secret: <c>pin_hash</c>, <c>pinHash</c>,
    /// <c>Pin-Hash</c> and <c>PINHASH</c> are all rejected.
    /// </summary>
    public static bool IsForbiddenName(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        if (AlwaysSecretNames.Contains(propertyName))
        {
            return true;
        }

        var tokens = Tokenise(propertyName);
        if (tokens.Any(ForbiddenTokens.Contains))
        {
            return true;
        }

        for (var i = 0; i + 1 < tokens.Length; i++)
        {
            if (ForbiddenTokenPairs.Contains($"{tokens[i]}+{tokens[i + 1]}"))
            {
                return true;
            }
        }

        var collapsed = Collapse(propertyName);
        return ForbiddenCompounds.Any(collapsed.Contains);
    }

    /// <summary>
    /// True when a string value looks like a secret regardless of the property
    /// name it sits under.
    /// </summary>
    public static bool IsSecretShapedValue(string? value) =>
        !string.IsNullOrEmpty(value)
        && (JwtLikeTokenRegex().IsMatch(value) || ConnectionStringFragmentRegex().IsMatch(value));

    /// <summary>
    /// Throws when a payload contains a secret by name or by shape.
    /// </summary>
    /// <param name="payload">
    /// An object graph about to be written to <c>AuditLog.BeforeJson</c> or
    /// <c>AfterJson</c>.
    /// </param>
    /// <param name="parameterName">For the exception message.</param>
    /// <exception cref="AuditPayloadSecretException">
    /// A forbidden property name or a secret-shaped value was found.
    /// </exception>
    public static void EnsureNoSecrets(object? payload, string parameterName = "payload")
    {
        ArgumentNullException.ThrowIfNull(parameterName);

        var problems = new List<string>();
        Inspect(payload, problems, depth: 0);

        if (problems.Count > 0)
        {
            throw new AuditPayloadSecretException(
                "The audit payload contains material that must never be persisted " +
                "(AGENTS.md §4). Remove the field from the payload rather than " +
                $"redacting it. Parameter: {parameterName}. Found: {string.Join("; ", problems)}");
        }
    }

    /// <summary>
    /// Bounds the walk. An unbounded traversal in the audit path would be a
    /// denial-of-service vector in the one path that must always be able to
    /// record, so nesting is capped and the remaining depth is simply not
    /// inspected.
    /// </summary>
    private const int MaxDepth = 12;

    private static void Inspect(object? value, List<string> problems, int depth)
    {
        if (value is null || depth > MaxDepth)
        {
            return;
        }

        switch (value)
        {
            case JsonElement element:
                InspectJson(element, problems, depth);
                break;

            case JsonDocument document:
                InspectJson(document.RootElement, problems, depth);
                break;

            case string text:
                if (IsSecretShapedValue(text))
                {
                    problems.Add(ConnectionStringFragmentRegex().IsMatch(text)
                        ? "a connection-string-shaped value"
                        : "a JWT-shaped value");
                }

                break;

            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    InspectEntry(entry.Key?.ToString(), entry.Value, problems, depth);
                }

                break;

            case IEnumerable sequence:
                foreach (var item in sequence)
                {
                    Inspect(item, problems, depth + 1);
                }

                break;

            default:
                InspectProperties(value, problems, depth);
                break;
        }
    }

    private static void InspectEntry(string? name, object? value, List<string> problems, int depth)
    {
        if (IsForbiddenName(name))
        {
            problems.Add($"property '{name}'");
            return;
        }

        Inspect(value, problems, depth + 1);
    }

    private static void InspectProperties(object value, List<string> problems, int depth)
    {
        foreach (var property in value.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead)
            {
                continue;
            }

            object? child;
            try
            {
                child = property.GetValue(value);
            }
            catch (TargetInvocationException)
            {
                // A property getter that throws must not break auditing; the field
                // is skipped rather than failing the whole request.
                continue;
            }

            InspectEntry(property.Name, child, problems, depth);
        }
    }

    private static void InspectJson(JsonElement element, List<string> problems, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    InspectEntry(property.Name, property.Value, problems, depth);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Inspect(item, problems, depth + 1);
                }

                break;

            case JsonValueKind.String:
                if (element.GetString() is { } text && IsSecretShapedValue(text))
                {
                    problems.Add(ConnectionStringFragmentRegex().IsMatch(text)
                        ? "a connection-string-shaped value"
                        : "a JWT-shaped value");
                }

                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Splits a property name into lower-cased words on separators
    /// (<c>pin_hash</c>, <c>pin-hash</c>) and on camelCase humps
    /// (<c>pinHash</c> → <c>pin</c>, <c>Hash</c>).
    /// </summary>
    private static string[] Tokenise(string name)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];

            if (!char.IsLetterOrDigit(ch))
            {
                Flush();
                continue;
            }

            // A lower-to-upper transition starts a new word; a run of capitals
            // followed by a lower-case letter keeps the last capital with its word
            // (PinHASHValue → pin, hash, value).
            if (char.IsUpper(ch) && current.Length > 0)
            {
                var previous = name[i - 1];
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (!char.IsUpper(previous) || nextIsLower)
                {
                    Flush();
                }
            }

            current.Append(char.ToLowerInvariant(ch));
        }

        Flush();
        return [.. tokens];
    }

    /// <summary>
    /// Strips every non-alphanumeric character and lower-cases, so a naming style
    /// cannot hide a compound such as <c>RefreshToken</c> or <c>refresh_token</c>.
    /// </summary>
    private static string Collapse(string name)
    {
        Span<char> buffer = stackalloc char[name.Length];
        var length = 0;
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[length++] = char.ToLowerInvariant(ch);
            }
        }

        return new string(buffer[..length]);
    }
}

/// <summary>
/// Thrown when an audit payload contains a secret. Deliberately does not inherit
/// from <see cref="Stockly.Application.Common.Errors.BusinessException"/>: it is a
/// developer defect, never a client error, so it must not be mapped to a
/// documented error code.
/// </summary>
public sealed class AuditPayloadSecretException : Exception
{
    /// <summary>Creates the exception.</summary>
    public AuditPayloadSecretException(string message) : base(message)
    {
    }
}