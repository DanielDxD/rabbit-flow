using RabbitMQ.Client;

namespace RabbitFlow.Abstractions;

public interface IRabbitMqConnectionProvider
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    ValueTask<IChannel> RentChannelAsync(CancellationToken cancellationToken = default);
    void ReturnChannel(IChannel channel);
    Task<IChannel> CreateDedicatedChannelAsync(CancellationToken cancellationToken = default);
}
