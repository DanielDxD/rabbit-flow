using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitMQ.Client;

namespace RabbitFlow.Topology;

public sealed class QueueBindingTopology : IRabbitMqTopology
{
    private readonly ConsumeOptions _options;

    public QueueBindingTopology(ConsumeOptions options)
    {
        _options = options;
    }

    public Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Exchange))
        {
            return Task.CompletedTask;
        }

        return channel.QueueBindAsync(
            queue: _options.Queue,
            exchange: _options.Exchange,
            routingKey: _options.BindingRoutingKey ?? string.Empty,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
