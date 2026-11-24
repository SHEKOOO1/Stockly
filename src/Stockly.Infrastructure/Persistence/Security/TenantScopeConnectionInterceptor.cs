using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Stockly.Infrastructure.Persistence.Security;

/// <summary>
/// Puts the authenticated scope into <c>SESSION_CONTEXT</c> on every physical connection
/// EF opens, and takes it out again before that connection is closed.
/// <para>
/// The database predicates are the enforcement mechanism (ADR-0051), so the scope has
/// to be present on the connection <em>before</em> the first statement runs. Doing it at
/// a call site, or in middleware, would leave every path that opens a connection without
/// going through that code unprotected.
/// </para>
/// <para>
/// <strong>Why <c>ConnectionClosing</c> and not <c>ConnectionClosed</c>.</strong>
/// <c>ConnectionClosed</c> is raised <em>after</em> the underlying connection has been
/// closed, so there is no session left to write to and no command that could run. The
/// clear therefore hooks <c>ConnectionClosing</c>, which is raised while the connection
/// is still open.
/// </para>
/// <para>
/// <strong>Why the clear is best effort.</strong> Setting on open is the guarantee that
/// makes isolation correct: every physical open rewrites both keys unconditionally, so a
/// stale scope can never survive into the next request even if a clear was lost. The clear
/// is defence in depth for a connection handed back to the pool and later used outside
/// EF. Consequently a clear that fails — a broken socket, a cancellation during shutdown —
/// must not turn into an unhandled exception that masks the original failure or turns a
/// pooled connection into a hard error. It is logged and suppressed. What is
/// <em>not</em> suppressed is the set on open: if the scope cannot be written, the
/// connection must not be used for anything.
/// </para>
/// </summary>
public sealed class TenantScopeConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantScopeConnectionInterceptor>? _logger;

    public TenantScopeConnectionInterceptor(
        ITenantContext tenantContext,
        ILogger<TenantScopeConnectionInterceptor>? logger = null)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        // Not suppressed. If the scope cannot be written the connection must not be used,
        // and every statement it ran would execute without a scope and fail closed anyway
        // -- but failing closed by accident is not the same as having scoped it
        // deliberately, so the failure is surfaced rather than hidden.
        TenantScopeSession.ApplyAsync(connection, _tenantContext).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await TenantScopeSession.ApplyAsync(connection, _tenantContext, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult ConnectionClosing(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        TryClear(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        await TryClearAsync(connection, CancellationToken.None);
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }

    private void TryClear(DbConnection connection)
    {
        try
        {
            TenantScopeSession.ClearAsync(connection).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No connection string, tenant id, token or other secret is logged here: only
            // the exception type, because the message can embed server details.
            _logger?.LogWarning(
                "Could not clear the tenant scope before closing a pooled connection ({ExceptionType}). "
                + "The scope is rewritten unconditionally on every open, so isolation is unaffected.",
                ex.GetType().Name);
        }
    }

    private async Task TryClearAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await TenantScopeSession.ClearAsync(connection, cancellationToken);
        }
        catch (Exception ex)
        {
            // Cancellation during shutdown is expected and is logged as such.
            _logger?.LogWarning(
                ex,
                "Could not clear the tenant scope before closing a pooled connection ({ExceptionType}). "
                + "The scope is rewritten unconditionally on every open, so isolation is unaffected.",
                ex.GetType().Name);
        }
    }
}