using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitFlow.Configuration;
using RabbitFlow.Connection;
using RabbitFlow.Tests.TestHelpers;
using RabbitMQ.Client;

namespace RabbitFlow.Tests.Connection;

public class RabbitMqConnectionProviderTests
{
    [Fact]
    public async Task GetConnectionAsync_ReturnsOpenInjectedConnection()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connection = ChannelMockFactory.CreateConnection(channelMock);
        var sut = CreateProvider();
        ReflectionHelper.SetPrivateField(sut, "_connection", connection.Object);
        ReflectionHelper.SetPrivateField(sut, "_channelPool", new ChannelPool(connection.Object, 4));

        var result = await sut.GetConnectionAsync();

        Assert.Same(connection.Object, result);
    }

    [Fact]
    public async Task RentChannelAsync_ReturnsChannelFromPool()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connection = ChannelMockFactory.CreateConnection(channelMock);
        var sut = CreateProvider();
        ReflectionHelper.SetPrivateField(sut, "_connection", connection.Object);
        ReflectionHelper.SetPrivateField(sut, "_channelPool", new ChannelPool(connection.Object, 4));

        var channel = await sut.RentChannelAsync();

        Assert.Same(channelMock.Object, channel);
    }

    [Fact]
    public async Task ReturnChannel_ReturnsChannelToPool()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connection = ChannelMockFactory.CreateConnection(channelMock);
        var pool = new ChannelPool(connection.Object, 4);
        var sut = CreateProvider();
        ReflectionHelper.SetPrivateField(sut, "_connection", connection.Object);
        ReflectionHelper.SetPrivateField(sut, "_channelPool", pool);

        var rented = await pool.RentAsync(CancellationToken.None);
        sut.ReturnChannel(rented);
        var reused = await pool.RentAsync(CancellationToken.None);

        Assert.Same(channelMock.Object, reused);
    }

    [Fact]
    public async Task CreateDedicatedChannelAsync_CreatesChannelFromConnection()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connection = ChannelMockFactory.CreateConnection(channelMock);
        var sut = CreateProvider();
        ReflectionHelper.SetPrivateField(sut, "_connection", connection.Object);
        ReflectionHelper.SetPrivateField(sut, "_channelPool", new ChannelPool(connection.Object, 4));

        var channel = await sut.CreateDedicatedChannelAsync();

        Assert.Same(channelMock.Object, channel);
    }

    [Fact]
    public async Task DisposeAsync_DisposesConnectionAndPool()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connection = ChannelMockFactory.CreateConnection(channelMock);
        var pool = new ChannelPool(connection.Object, 2);
        var rented = await pool.RentAsync(CancellationToken.None);
        pool.Return(rented);

        var sut = CreateProvider();
        ReflectionHelper.SetPrivateField(sut, "_connection", connection.Object);
        ReflectionHelper.SetPrivateField(sut, "_channelPool", pool);

        await sut.DisposeAsync();

        connection.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task GetConnectionAsync_ThrowsWhenDisposed()
    {
        var sut = CreateProvider();
        await sut.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.GetConnectionAsync());
    }

    private static RabbitMqConnectionProvider CreateProvider() =>
        new(Options.Create(new RabbitFlowOptions()), NullLogger<RabbitMqConnectionProvider>.Instance);
}
