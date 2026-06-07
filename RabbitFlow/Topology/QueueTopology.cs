using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitMQ.Client;

namespace RabbitFlow.Topology;

public sealed class QueueTopology : IRabbitMqTopology
{
    private readonly ConsumeOptions _options;

    public QueueTopology(ConsumeOptions options)
    {
        _options = options;
    }

    public Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default) =>
        channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: _options.DurableQueue,
            exclusive: _options.ExclusiveQueue,
            autoDelete: _options.AutoDeleteQueue,
            arguments: null,
            cancellationToken: cancellationToken);
}
