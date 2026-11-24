using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stockly.Application.Common.Errors;

namespace Stockly.Api.Common;

/// <summary>
/// The single funnel for every unhandled exception, mapping it to an RFC 9457
/// body.
/// <para>
/// The mapping is a closed list. Anything not on it becomes
/// <see cref="ApiProblem.Internal"/> — a fixed sentence plus a trace id — because
/// §6.3 forbids a <c>500</c> carrying an exception message, and an unmapped
/// exception must not become a 500 that carries one.
/// </para>
/// <para>
/// This is where S22 and S13 (<c>ErrorSafetyTests</c>, <c>SecretLeakTests</c>) are
/// enforced in code rather than by review: the exception object never reaches the
/// response, only the catalogue does.
/// </para>
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ProblemDetailsWriter _writer;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GlobalExceptionHandler(ProblemDetailsWriter writer, ILogger<GlobalExceptionHandler> logger)
    {
        _writer = writer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = CorrelationIdMiddleware.Get(context);
        var instance = context.Request.Path.HasValue ? context.Request.Path.Value ?? string.Empty : string.Empty;

        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // The caller hung up. Writing a body would fail, and logging it as an
            // error would bury real failures under disconnect noise.
            _logger.LogDebug("Request aborted by the caller. traceId={TraceId}", traceId);
            return false;
        }

        var problem = Map(exception, traceId, instance);

        if (problem.Status >= 500)
        {
            // The exception itself is logged with its trace id; the user gets only
            // the id. This is the one place the full detail is allowed to exist.
            _logger.LogError(
                exception,
                "Unhandled failure on {Method} {Path}. traceId={TraceId}",
                context.Request.Method,
                instance,
                traceId);
        }
        else
        {
            _logger.LogInformation(
                "Request rejected with {Code} on {Method} {Path}. traceId={TraceId}",
                problem.Code,
                context.Request.Method,
                instance,
                traceId);
        }

        if (context.Response.HasStarted)
        {
            // The status line is already on the wire; writing a body now would
            // corrupt the response. Log and stop rather than attempt a partial
            // write.
            _logger.LogWarning(
                "Response already started; cannot write problem detail. traceId={TraceId}", traceId);
            return false;
        }

        await _writer.WriteAsync(context, problem);
        return true;
    }

    /// <summary>
    /// The closed mapping. Exposed as a static so a unit test can assert the whole
    /// table without going through HTTP.
    /// </summary>
    internal static ApiProblem Map(Exception exception, string traceId, string instance) =>
        exception switch
        {
            BusinessException business => ApiProblem.Create(
                business.Code, traceId, business.Detail, instance, business.Errors),

            // A `rowversion` that no longer matches is a 412, not a 500. The 428
            // "If-Match missing" case is decided earlier, by the service, before
            // any update is attempted — so reaching here means the token was sent
            // and did not match.
            DbUpdateConcurrencyException => ApiProblem.Create(
                BusinessErrorCode.ConcurrencyConflict,
                traceId,
                "The resource changed since it was read. Re-read it and retry.",
                instance),

            // An infrastructure failure is 503, not 500: it is expected to recover
            // without operator action. The detail never names the database or the
            // host (S22).
            DbUpdateException or
            TimeoutException => ApiProblem.DependencyUnavailable(traceId, instance),

            BadHttpRequestException bad => ApiProblem.Create(
                BusinessErrorCode.ValidationFailed, traceId, "The request could not be read.", instance),

            _ => ApiProblem.Internal(traceId, instance),
        };
}