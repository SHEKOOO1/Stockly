using Stockly.Infrastructure.Outbox;

namespace Stockly.Api.Outbox;

public interface IOutboxMessageHandler
{
    string MessageType { get; }

    Task HandleAsync(ClaimedOutboxMessage message, CancellationToken cancellationToken);
}
