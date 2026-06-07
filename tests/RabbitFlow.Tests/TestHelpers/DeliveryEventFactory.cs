using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitFlow.Tests.TestHelpers;

internal static class DeliveryEventFactory
{
    public static BasicDeliverEventArgs Create(ReadOnlyMemory<byte> body, ulong deliveryTag = 1) =>
        new(
            consumerTag: "test-consumer",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "orders",
            routingKey: "order.created",
            properties: new BasicProperties(),
            body: body,
            cancellationToken: CancellationToken.None);
}
