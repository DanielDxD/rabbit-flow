using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace RabbitFlow.Connection;

internal sealed class ChannelPool : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly int _maxSize;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentBag<IChannel> _channels = [];
    private int _disposed;

    public ChannelPool(IConnection connection, int maxSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxSize, 0);

        _connection = connection;
        _maxSize = maxSize;
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    public async ValueTask<IChannel> RentAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (_channels.TryTake(out var channel) && channel.IsOpen)
        {
            return channel;
        }

        if (channel is null)
            return await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        
        await channel.CloseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.DisposeAsync().ConfigureAwait(false);

        return await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public void Return(IChannel channel)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            channel.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        if (channel.IsOpen)
        {
            _channels.Add(channel);
        }
        else
        {
            channel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _semaphore.Release();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        while (_channels.TryTake(out var channel))
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync().ConfigureAwait(false);
            }

            await channel.DisposeAsync().ConfigureAwait(false);
        }

        _semaphore.Dispose();
    }
}
