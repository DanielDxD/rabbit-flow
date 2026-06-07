using RabbitMQ.Client;

namespace RabbitFlow.Abstractions;

public interface IRabbitMqTopology
{
    Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default);
}
