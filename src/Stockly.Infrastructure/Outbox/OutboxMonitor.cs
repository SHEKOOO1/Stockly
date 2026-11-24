using System.Data;
using Microsoft.Data.SqlClient;
using Stockly.Infrastructure.Persistence.Security;

namespace Stockly.Infrastructure.Outbox;

public sealed record OutboxSnapshot(int PendingOrProcessing, int Stale, int DeadLetter, bool MonitorConfigured)
{
    public bool IsHealthy(OutboxOptions options) =>
        MonitorConfigured && Stale <= options.MaxStaleBacklog && DeadLetter == 0;

    public string Describe(OutboxOptions options) =>
        !MonitorConfigured ? "unavailable" : IsHealthy(options) ? "ok" : "degraded";
}

public sealed class OutboxMonitor
{
    private readonly string? _connectionString;
    private readonly OutboxOptions _options;

    public OutboxMonitor(string? connectionString, OutboxOptions options)
    {
        _connectionString = connectionString;
        _options = options;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<OutboxSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new OutboxSnapshot(0, 0, 0, MonitorConfigured: false);
        }

        var cutoff = DateTime.UtcNow.AddMinutes(-_options.StaleProcessingMinutes);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await TenantScopeSession.ApplyPlatformAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
    ISNULL(SUM(CASE WHEN [Status] IN (1, 2) THEN 1 ELSE 0 END), 0),
    ISNULL(SUM(CASE WHEN [Status] IN (1, 2) AND [CreatedAtUtc] < @cutoff THEN 1 ELSE 0 END), 0),
    ISNULL(SUM(CASE WHEN [Status] = 5 THEN 1 ELSE 0 END), 0)
FROM [ops].[OutboxMessage]
""";
        command.Parameters.Add(new SqlParameter("@cutoff", SqlDbType.DateTime2) { Value = cutoff });

        var values = new int[3];
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            values[0] = reader.GetInt32(0);
            values[1] = reader.GetInt32(1);
            values[2] = reader.GetInt32(2);
        }

        return new OutboxSnapshot(values[0], values[1], values[2], MonitorConfigured: true);
    }
}
