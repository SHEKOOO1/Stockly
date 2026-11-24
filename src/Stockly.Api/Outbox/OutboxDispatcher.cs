using Microsoft.Extensions.Hosting;
using Stockly.Infrastructure.Outbox;

namespace Stockly.Api.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly OutboxCoordinator _coordinator;
    private readonly OutboxOptions _options;
    private readonly IReadOnlyList<IOutboxMessageHandler> _handlers;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        OutboxCoordinator coordinator,
        OutboxOptions options,
        IEnumerable<IOutboxMessageHandler> handlers,
        ILogger<OutboxDispatcher> logger)
    {
        _coordinator = coordinator;
        _options = options;
        _handlers = handlers.ToArray();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!_coordinator.IsConfigured)
        {
            _logger.LogWarning("Outbox dispatcher is disabled: no worker connection is configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Outbox batch failed ({ExceptionType}).", ex.GetType().Name);
            }

            await Task.Delay(_options.PollInterval, stoppingToken);
        }
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var claimed = await _coordinator.ClaimBatchAsync(cancellationToken);
        var processed = 0;

        foreach (var message in claimed)
        {
            var handler = _handlers.FirstOrDefault(
                candidate => string.Equals(candidate.MessageType, message.MessageType, StringComparison.Ordinal));

            if (handler is null)
            {
                await _coordinator.ReleaseUnhandledAsync(message.Id, cancellationToken);
                continue;
            }

            try
            {
                await handler.HandleAsync(message, cancellationToken);
                if (await _coordinator.MarkProcessedAsync(message.Id, cancellationToken))
                {
                    processed++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await _coordinator.MarkFailedAsync(
                    message.Id,
                    message.Attempts,
                    ex.GetType().Name,
                    cancellationToken);
            }
        }

        return processed;
    }
}
