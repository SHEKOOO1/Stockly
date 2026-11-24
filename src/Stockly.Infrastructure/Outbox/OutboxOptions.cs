namespace Stockly.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";
    public const string MonitorConnectionName = "StocklyOutboxMonitor";
    public const string MonitorConnectionEnvironmentVariable = "ConnectionStrings__StocklyOutboxMonitor";
    public const string WorkerConnectionName = "StocklyOutboxWorker";
    public const string WorkerConnectionEnvironmentVariable = "ConnectionStrings__StocklyOutboxWorker";
    public const int DefaultStaleProcessingMinutes = 15;
    public const int DefaultMaxStaleBacklog = 0;
    public const int DefaultBatchSize = 50;
    public const int DefaultPollIntervalSeconds = 5;
    public const int DefaultMaxAttempts = 8;
    public const int DefaultLeaseSeconds = 60;

    public int BatchSize { get; set; } = DefaultBatchSize;

    public int PollIntervalSeconds { get; set; } = DefaultPollIntervalSeconds;

    public int MaxAttempts { get; set; } = DefaultMaxAttempts;

    public int StaleProcessingMinutes { get; set; } = DefaultStaleProcessingMinutes;

    public int MaxStaleBacklog { get; set; } = DefaultMaxStaleBacklog;

    public int LeaseSeconds { get; set; } = DefaultLeaseSeconds;

    public bool Enabled { get; set; } = true;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);

    public TimeSpan Lease => TimeSpan.FromSeconds(LeaseSeconds);

    public void Validate()
    {
        if (BatchSize is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize));
        }

        if (PollIntervalSeconds is < 1 or > 3600)
        {
            throw new ArgumentOutOfRangeException(nameof(PollIntervalSeconds));
        }

        if (MaxAttempts is < 1 or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts));
        }

        if (StaleProcessingMinutes is < 1 or > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(StaleProcessingMinutes));
        }

        if (MaxStaleBacklog < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxStaleBacklog));
        }

        if (LeaseSeconds is < 1 or > 3600)
        {
            throw new ArgumentOutOfRangeException(nameof(LeaseSeconds));
        }
    }

    public TimeSpan BackoffAfter(int attempts)
    {
        var exponent = Math.Clamp(attempts, 1, 12);
        return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, exponent), 3600));
    }
}
