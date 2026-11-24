using System.Text.Json;
using System.Text.Json.Serialization;
using Stockly.Application.Common.Errors;

namespace Stockly.Api.Common;

/// <summary>
/// Serialises an <see cref="ApiProblem"/> as RFC 9457
/// <c>application/problem+json</c> and writes it to the response.
/// <para>
/// The body is serialised directly rather than through
/// <c>IProblemDetailsService</c>, because S22 depends on the exact bytes: the
/// framework service applies its own conventions to <c>ProblemDetails</c>
/// extensions, and a problem body is the one response where an unexpected member
/// — a nested exception, an inner error, a host name — is a defect.
/// </para>
/// <para>
/// <see cref="ApiProblem"/> has no <c>Exception</c> and no free-form server text
/// beyond the caller-supplied <c>detail</c>, so a stack trace, SQL fragment or
/// internal type name has no route into the body. That makes error safety a
/// structural property of this type rather than a review checklist.
/// </para>
/// </summary>
public sealed class ProblemDetailsWriter
{
    /// <summary>Shared, thread-safe options; the writer is stateless.</summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Writes <paramref name="problem"/> to the response.</summary>
    public async Task WriteAsync(HttpContext context, ApiProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(problem);

        context.Response.StatusCode = problem.Status;
        context.Response.ContentType = ApiProblem.ContentType;

        // §5.1: responses are non-cacheable. An error body that a shared terminal
        // could cache would outlive the session that produced it.
        context.Response.Headers.CacheControl = "no-store";

        if (problem.Status == StatusCodes.Status429TooManyRequests)
        {
            // §5.1 lists Retry-After as a required response header for throttling.
            // The real value comes from the rate limiter; a placeholder keeps the
            // header present until that exists.
            context.Response.Headers.RetryAfter = "1";
        }

        await context.Response.WriteAsJsonAsync(problem, SerializerOptions, "application/problem+json");
    }

    /// <summary>
    /// Serialises a problem without a response. Used by the unit tests that assert
    /// the exact error body, and by anything that needs to hash or log the body.
    /// </summary>
    public static string Serialize(ApiProblem problem) =>
        JsonSerializer.Serialize(problem, SerializerOptions);
}