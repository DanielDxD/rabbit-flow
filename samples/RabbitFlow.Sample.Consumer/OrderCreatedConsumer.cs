using RabbitFlow.Messaging;
using RabbitFlow.Samples.Contracts;

namespace RabbitFlow.Sample.Consumer;

public sealed class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : Consumer<OrderCreatedEvent>
{
    public override Task ConsumeAsync(OrderCreatedEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Received order {OrderId} from {CustomerName} — total {Total:C}",
            message.OrderId,
            message.CustomerName,
            message.Total);

        return Task.CompletedTask;
    }
}
