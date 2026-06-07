using RabbitFlow.Abstractions;

namespace RabbitFlow.Messaging;

public abstract class Consumer<TMessage> : IConsumer<TMessage>
{
    public abstract Task ConsumeAsync(TMessage message, CancellationToken cancellationToken = default);
}
