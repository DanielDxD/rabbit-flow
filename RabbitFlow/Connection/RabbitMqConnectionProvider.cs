using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitMQ.Client;

namespace RabbitFlow.Connection;

internal sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private readonly RabbitFlowOptions _options;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private ChannelPool? _channelPool;
    private int _disposed;

    public RabbitMqConnectionProvider(
        IOptions<RabbitFlowOptions> options,
        ILogger<RabbitMqConnectionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _factory = CreateFactory(_options);
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }

            _connection = await _factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            _channelPool = new ChannelPool(_connection, _options.ChannelPoolSize);

            _logger.LogInformation(
                "RabbitMQ connection established to {HostName}:{Port}/{VirtualHost}",
                _options.HostName,
                _options.Port,
                _options.VirtualHost);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask<IChannel> RentChannelAsync(CancellationToken cancellationToken = default)
    {
        await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await _channelPool!.RentAsync(cancellationToken).ConfigureAwait(false);
    }

    public void ReturnChannel(IChannel channel) => _channelPool!.Return(channel);

    public async Task<IChannel> CreateDedicatedChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_channelPool is not null)
        {
            await _channelPool.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _connectionLock.Dispose();
    }

    private static ConnectionFactory CreateFactory(RabbitFlowOptions options) => new()
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        ClientProvidedName = options.ClientProvidedName,
        AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
        TopologyRecoveryEnabled = options.TopologyRecoveryEnabled,
        NetworkRecoveryInterval = options.ConnectionRecoveryInterval
    };
}
