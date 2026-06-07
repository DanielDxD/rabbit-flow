namespace RabbitFlow.Abstractions;

public interface IConsumer<in TMessage>
{
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken = default);
}
