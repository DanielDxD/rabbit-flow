using Microsoft.Extensions.Logging;
using RabbitFlow.Abstractions;
using RabbitFlow.Messaging;
using RabbitMQ.Client;

namespace RabbitFlow.Publishing;

internal sealed class RabbitMqPublisher : IPublisher
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IRabbitMqConnectionProvider connectionProvider,
        IMessageSerializer serializer,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _serializer = serializer;
        _logger = logger;
    }

    public Task PublishAsync<T>(
        T message,
        PublishContext? context = null,
        CancellationToken cancellationToken = default) =>
        PublishAsync(message, string.Empty, string.Empty, context, cancellationToken);

    public async Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        PublishContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        context ??= new PublishContext();
        var body = _serializer.Serialize(message);
        var properties = new BasicProperties
        {
            Persistent = context.Persistent,
            ContentType = context.ContentType ?? "application/json",
            CorrelationId = context.CorrelationId,
            MessageId = context.MessageId ?? Guid.NewGuid().ToString("N"),
            Headers = context.Headers?.ToDictionary(static pair => pair.Key, static pair => pair.Value)
        };

        var channel = await _connectionProvider.RentChannelAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: context.Mandatory,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Published message {MessageType} to exchange '{Exchange}' with routing key '{RoutingKey}'",
                typeof(T).Name,
                exchange,
                routingKey);
        }
        finally
        {
            _connectionProvider.ReturnChannel(channel);
        }
    }
}
