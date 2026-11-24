namespace Stockly.Application.Auditing;

/// <summary>
/// Collects audit entries and hands them to the caller to write inside the
/// business transaction.
/// <para>
/// This deliberately does <strong>not</strong> save anything itself. Rule 6 of the
/// audit model requires the row to commit or roll back with the business change,
/// which means the row has to join the caller's <c>DbContext</c> change set. A
/// writer that called <c>SaveChangesAsync</c> itself could not honour that.
/// </para>
/// <para>
/// Registering the implementation with a scoped lifetime makes the collector
/// request-scoped, so entries cannot leak between requests the way a static buffer
/// would.
/// </para>
/// </summary>
public interface IAuditCollector
{
    /// <summary>
    /// Stages an entry. Nothing is written yet; the caller drains the buffer with
    /// <see cref="Drain"/> inside its transaction.
    /// </summary>
    void Add(AuditEntry entry);

    /// <summary>
    /// Returns everything staged and clears the buffer, so a drained entry cannot
    /// be written twice if the same collector is drained again in the same request.
    /// </summary>
    IReadOnlyList<AuditEntry> Drain();
}

/// <summary>The default request-scoped collector.</summary>
public sealed class AuditCollector : IAuditCollector
{
    private readonly List<AuditEntry> _entries = [];

    /// <inheritdoc />
    public void Add(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }

    /// <inheritdoc />
    public IReadOnlyList<AuditEntry> Drain()
    {
        if (_entries.Count == 0)
        {
            return [];
        }

        var drained = _entries.ToArray();
        _entries.Clear();
        return drained;
    }
}