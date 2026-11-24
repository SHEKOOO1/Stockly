using System.Data;
using Microsoft.Data.SqlClient;
using Stockly.Infrastructure.Persistence.Security;

namespace Stockly.Infrastructure.Outbox;

public sealed record ClaimedOutboxMessage(
    Guid Id,
    Guid? TenantId,
    string MessageType,
    string Payload,
    short Attempts);

public sealed class OutboxCoordinator
{
    private readonly string? _connectionString;
    private readonly OutboxOptions _options;

    public OutboxCoordinator(string? connectionString, OutboxOptions options)
    {
        _connectionString = connectionString;
        _options = options;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<IReadOnlyList<ClaimedOutboxMessage>> ClaimBatchAsync(
        CancellationToken cancellationToken = default)
    {
        RequireConfiguration();
        var now = DateTime.UtcNow;
        var claimed = new List<ClaimedOutboxMessage>();

        await using var connection = await OpenScopedAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE TOP (@batch) o WITH (UPDLOCK, ROWLOCK)
SET [Status] = 2,
    [LockedUntilUtc] = DATEADD(second, @lease, @now),
    [Attempts] = [Attempts] + 1
OUTPUT inserted.[Id], inserted.[TenantId], inserted.[MessageType], inserted.[Payload], inserted.[Attempts]
FROM [ops].[OutboxMessage] AS o
WHERE [Attempts] < @maxAttempts
  AND (
        ([Status] IN (1, 4) AND [NextAttemptAtUtc] <= @now)
        OR ([Status] = 2 AND [LockedUntilUtc] <= @now)
      )
""";
        AddParameter(command, "@batch", SqlDbType.Int, _options.BatchSize);
        AddParameter(command, "@lease", SqlDbType.Int, _options.LeaseSeconds);
        AddParameter(command, "@now", SqlDbType.DateTime2, now);
        AddParameter(command, "@maxAttempts", SqlDbType.SmallInt, (short)_options.MaxAttempts);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            claimed.Add(new ClaimedOutboxMessage(
                reader.GetGuid(0),
                reader.IsDBNull(1) ? null : reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt16(4)));
        }

        return claimed;
    }

    public Task<bool> MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequireConfiguration();
        return ExecuteTransitionAsync(
            """
UPDATE [ops].[OutboxMessage]
SET [Status] = 3, [ProcessedAtUtc] = @now, [LockedUntilUtc] = NULL, [LastError] = NULL
WHERE [Id] = @id AND [Status] = 2 AND [LockedUntilUtc] > @now
""",
            id,
            DateTime.UtcNow,
            null,
            null,
            cancellationToken);
    }

    public Task<bool> MarkFailedAsync(Guid id, short attempts, string safeError, CancellationToken cancellationToken = default)
    {
        RequireConfiguration();
        var now = DateTime.UtcNow;
        var deadLetter = attempts >= _options.MaxAttempts;
        var status = deadLetter ? 5 : 4;
        var nextAttempt = deadLetter ? now : now.Add(_options.BackoffAfter(attempts));
        var error = safeError.Length <= 2000 ? safeError : safeError[..2000];

        return ExecuteTransitionAsync(
            """
UPDATE [ops].[OutboxMessage]
SET [Status] = @status, [NextAttemptAtUtc] = @next, [LockedUntilUtc] = NULL, [LastError] = @error
WHERE [Id] = @id AND [Status] = 2 AND [LockedUntilUtc] > @now
""",
            id,
            now,
            status,
            (nextAttempt, error),
            cancellationToken);
    }

    public Task<bool> ReleaseUnhandledAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequireConfiguration();
        var now = DateTime.UtcNow;

        return ExecuteTransitionAsync(
            """
UPDATE [ops].[OutboxMessage]
SET [Status] = 1, [NextAttemptAtUtc] = @next, [LockedUntilUtc] = NULL, [Attempts] = [Attempts] - 1
WHERE [Id] = @id AND [Status] = 2 AND [LockedUntilUtc] > @now
""",
            id,
            now,
            null,
            (now.Add(_options.PollInterval), (string?)null),
            cancellationToken);
    }

    private async Task<SqlConnection> OpenScopedAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await TenantScopeSession.ApplyPlatformAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private async Task<bool> ExecuteTransitionAsync(
        string sql,
        Guid id,
        DateTime now,
        int? status,
        (DateTime Next, string? Error)? failure,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenScopedAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@id", SqlDbType.UniqueIdentifier, id);
        AddParameter(command, "@now", SqlDbType.DateTime2, now);

        if (status is not null)
        {
            AddParameter(command, "@status", SqlDbType.Int, status.Value);
        }

        if (failure is { } value)
        {
            AddParameter(command, "@next", SqlDbType.DateTime2, value.Next);
            AddParameter(command, "@error", SqlDbType.NVarChar, (object?)value.Error ?? DBNull.Value);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static void AddParameter(SqlCommand command, string name, SqlDbType type, object value) =>
        command.Parameters.Add(new SqlParameter(name, type) { Value = value });

    private void RequireConfiguration()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "No outbox worker connection string is configured. Set "
                + OutboxOptions.WorkerConnectionEnvironmentVariable
                + " to a write-capable principal in stockly_platform_role.");
        }
    }
}
