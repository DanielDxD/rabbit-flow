using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitFlow.Tests.TestHelpers;

internal static class ConsumerEventHelper
{
    public static Task InvokeReceivedAsync(
        AsyncEventingBasicConsumer consumer,
        BasicDeliverEventArgs args) =>
        consumer.HandleBasicDeliverAsync(
            args.ConsumerTag,
            args.DeliveryTag,
            args.Redelivered,
            args.Exchange,
            args.RoutingKey,
            args.BasicProperties,
            args.Body,
            args.CancellationToken);
}
