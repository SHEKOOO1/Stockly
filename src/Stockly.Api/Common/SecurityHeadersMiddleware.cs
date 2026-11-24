namespace Stockly.Api.Common;

/// <summary>
/// Emits the response headers listed in <c>SECURITY_ARCHITECTURE.md</c> §12.
/// <para>
/// These are set here rather than at the edge only because a header set by Nginx can
/// be <em>removed</em> by the app and one set by the app can be dropped in a
/// misconfigured proxy, so both layers have to agree. Application-level setting is
/// the one that survives a direct-to-container request.
/// </para>
/// <para>
/// The values are constants rather than configuration on purpose: a weaker value
/// must not be expressible by a deployment that is simply misconfigured. Adding a
/// knob here would reintroduce exactly the class of defect §12 exists to prevent.
/// </para>
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private static readonly string[] Permissions =
    [
        "camera=(self)",
        "microphone=()",
        "geolocation=()",
    ];

    /// <summary>
    /// The exact <c>Content-Security-Policy</c> from §12.
    /// <para>
    /// <c>frame-ancestors 'none'</c> and <c>object-src 'none'</c> are the two
    /// directives that matter most for a data-integrity system: they stop a
    /// warehouse terminal from being framed for clickjacking and stop plugin content
    /// from being smuggled in.
    /// </para>
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: blob:; " +
        "connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; " +
        "form-action 'self'";

    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Runs the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;

        // HSTS only over TLS. Sending it over plain HTTP is ignored by browsers and
        // would suggest a false sense of protection during local development.
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = string.Join(", ", Permissions);
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        // Anything behind authentication must not be cached by a shared proxy. A
        // cached response would serve one operator's data to the next user of the
        // same terminal.
        if (RequiresNoStore(context))
        {
            headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            headers["Pragma"] = "no-cache";
        }

        await _next(context);
    }

    /// <summary>
    /// Whether the response belongs to an authenticated request.
    /// <para>
    /// Presence of a bearer token is the trigger. An anonymous route such as
    /// <c>/health</c> is left cacheable so a probe is not defeated by an intermediary.
    /// </para>
    /// </summary>
    private static bool RequiresNoStore(HttpContext context)
    {
        return !string.IsNullOrWhiteSpace(context.Request.Headers.Authorization);
    }
}