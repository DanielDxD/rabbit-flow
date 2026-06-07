using RabbitFlow.Messaging;

namespace RabbitFlow.Abstractions;

public interface ITypedPublisher<in TMessage>
{
    Task PublishAsync(
        TMessage message,
        PublishContext? context = null,
        CancellationToken cancellationToken = default);
}
