using RabbitFlow.Messaging;

namespace RabbitFlow.Abstractions;

public interface IPublisher
{
    Task PublishAsync<T>(
        T message,
        PublishContext? context = null,
        CancellationToken cancellationToken = default);

    Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        PublishContext? context = null,
        CancellationToken cancellationToken = default);
}
