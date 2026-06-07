using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitMQ.Client;

namespace RabbitFlow.Topology;

public sealed class ExchangeTopology : IRabbitMqTopology
{
    private readonly PublishOptions _options;
    private readonly string _exchangeType;

    public ExchangeTopology(PublishOptions options, string exchangeType = ExchangeType.Topic)
    {
        _options = options;
        _exchangeType = exchangeType;
    }

    public Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Exchange))
        {
            return Task.CompletedTask;
        }

        return channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: _exchangeType,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
