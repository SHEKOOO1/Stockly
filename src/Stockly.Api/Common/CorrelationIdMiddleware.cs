using System.Diagnostics;

namespace Stockly.Api.Common;

/// <summary>
/// Assigns and propagates <c>X-Correlation-Id</c> (<c>API_CONVENTIONS.md</c> §5.1):
/// echoed when the caller supplies one, generated when absent.
/// <para>
/// The id is stored in <see cref="HttpContext.Items"/> so that the audit writer,
/// the problem writer and the logs all quote the same value as the
/// <c>traceId</c> a user is asked to read out.
/// </para>
/// <para>
/// An inbound id is bounded and sanitised before it is trusted, because it is
/// echoed into a response header and into log lines. An unbounded value would
/// let a caller inflate the log and the header.
/// </para>
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>The header name, per §5.1.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The <see cref="HttpContext.Items"/> key.</summary>
    public const string ItemKey = "stockly.correlation-id";

    private const int MaxLength = 100;

    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware.</summary>
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Runs the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveInbound(context.Request.Headers[HeaderName]);

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // Baggage so the value is queryable in telemetry. It is not a security
        // boundary and no decision is ever made from it.
        Activity.Current?.SetBaggage(HeaderName, correlationId);

        await _next(context);
    }

    /// <summary>
    /// Returns the caller's id when it is usable, otherwise a fresh one.
    /// </summary>
    private static string ResolveInbound(string? inbound)
    {
        if (string.IsNullOrWhiteSpace(inbound))
        {
            return Guid.NewGuid().ToString();
        }

        var trimmed = inbound.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Guid.NewGuid().ToString();
        }

        foreach (var ch in trimmed)
        {
            // Header-safe characters only. Anything else could split the header or
            // inject a line break into a log line downstream.
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not ('-' or '_' or '.' or ':'))
            {
                return Guid.NewGuid().ToString();
            }
        }

        return trimmed;
    }

    /// <summary>
    /// The correlation id for the current request, generating one if the
    /// middleware has not run (for example in a unit test that builds an
    /// <see cref="DefaultHttpContext"/> directly).
    /// </summary>
    public static string Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var value)
            && value is string existing
            && !string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        var generated = Guid.NewGuid().ToString();
        context.Items[ItemKey] = generated;
        return generated;
    }
}