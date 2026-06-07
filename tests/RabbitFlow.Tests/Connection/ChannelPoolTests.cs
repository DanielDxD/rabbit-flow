using Moq;
using RabbitFlow.Connection;
using RabbitMQ.Client;

namespace RabbitFlow.Tests.Connection;

public class ChannelPoolTests
{
    [Fact]
    public void Constructor_ThrowsWhenMaxSizeIsZero()
    {
        var connection = new Mock<IConnection>();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ChannelPool(connection.Object, 0));
    }

    [Fact]
    public async Task RentAsync_CreatesChannelWhenPoolIsEmpty()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        var connection = new Mock<IConnection>();
        connection.Setup(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);

        await using var pool = new ChannelPool(connection.Object, 2);
        var rented = await pool.RentAsync(CancellationToken.None);

        Assert.Same(channel.Object, rented);
        connection.Verify(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnAndRent_ReusesOpenChannel()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        var connection = new Mock<IConnection>();
        connection.Setup(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);

        await using var pool = new ChannelPool(connection.Object, 2);
        var first = await pool.RentAsync(CancellationToken.None);
        pool.Return(first);
        var second = await pool.RentAsync(CancellationToken.None);

        Assert.Same(channel.Object, second);
        connection.Verify(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RentAsync_CreatesNewChannelWhenReturnedChannelIsClosed()
    {
        var closedChannel = new Mock<IChannel>();
        closedChannel.SetupGet(c => c.IsOpen).Returns(false);
        closedChannel.Setup(c => c.CloseAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        closedChannel.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var openChannel = new Mock<IChannel>();
        openChannel.SetupGet(c => c.IsOpen).Returns(true);

        var connection = new Mock<IConnection>();
        connection.SetupSequence(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(closedChannel.Object)
            .ReturnsAsync(openChannel.Object);

        await using var pool = new ChannelPool(connection.Object, 2);
        var first = await pool.RentAsync(CancellationToken.None);
        pool.Return(first);
        var second = await pool.RentAsync(CancellationToken.None);

        Assert.Same(openChannel.Object, second);
        connection.Verify(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DisposeAsync_ClosesPooledChannels()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.CloseAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        channel.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var connection = new Mock<IConnection>();
        connection.Setup(c => c.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);

        var pool = new ChannelPool(connection.Object, 2);
        var rented = await pool.RentAsync(CancellationToken.None);
        pool.Return(rented);

        await pool.DisposeAsync();

        channel.Verify(c => c.CloseAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task RentAsync_ThrowsWhenDisposed()
    {
        var connection = new Mock<IConnection>();
        var pool = new ChannelPool(connection.Object, 1);
        await pool.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            pool.RentAsync(CancellationToken.None).AsTask());
    }
}
