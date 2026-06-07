using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitFlow.Abstractions;
using RabbitFlow.Messaging;
using RabbitFlow.Publishing;
using RabbitFlow.Serialization;
using RabbitFlow.Tests.TestHelpers;
using RabbitMQ.Client;

namespace RabbitFlow.Tests.Publishing;

public class RabbitMqPublisherTests
{
    [Fact]
    public async Task PublishAsync_RentsChannel_PublishesAndReturnsChannel()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        connectionProvider.Setup(p => p.RentChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var sut = new RabbitMqPublisher(
            connectionProvider.Object,
            new JsonMessageSerializer(),
            NullLogger<RabbitMqPublisher>.Instance);

        var message = new TestMessage(Guid.NewGuid(), "Publish");

        await sut.PublishAsync(message, "orders", "order.created");

        channelMock.Verify(c => c.BasicPublishAsync(
            "orders",
            "order.created",
            false,
            It.Is<BasicProperties>(p =>
                p.ContentType == "application/json" &&
                p.Persistent &&
                !string.IsNullOrWhiteSpace(p.MessageId)),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        connectionProvider.Verify(p => p.ReturnChannel(channelMock.Object), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_UsesDefaultExchangeAndRoutingKey()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        connectionProvider.Setup(p => p.RentChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var sut = new RabbitMqPublisher(
            connectionProvider.Object,
            new JsonMessageSerializer(),
            NullLogger<RabbitMqPublisher>.Instance);

        await sut.PublishAsync(new TestMessage(Guid.NewGuid(), "Default"));

        channelMock.Verify(c => c.BasicPublishAsync(
            string.Empty,
            string.Empty,
            false,
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ThrowsWhenMessageIsNull()
    {
        var sut = new RabbitMqPublisher(
            Mock.Of<IRabbitMqConnectionProvider>(),
            new JsonMessageSerializer(),
            NullLogger<RabbitMqPublisher>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_AppliesPublishContext()
    {
        var (channelMock, _) = ChannelMockFactory.CreateConsumingChannel();
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        connectionProvider.Setup(p => p.RentChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var sut = new RabbitMqPublisher(
            connectionProvider.Object,
            new JsonMessageSerializer(),
            NullLogger<RabbitMqPublisher>.Instance);

        var context = new PublishContext
        {
            CorrelationId = "corr-1",
            MessageId = "msg-1",
            ContentType = "application/vnd.test+json",
            Persistent = false,
            Mandatory = true,
            Headers = new Dictionary<string, object?> { ["source"] = "test" }
        };

        await sut.PublishAsync(new TestMessage(Guid.NewGuid(), "Ctx"), "ex", "rk", context);

        channelMock.Verify(c => c.BasicPublishAsync(
            "ex",
            "rk",
            true,
            It.Is<BasicProperties>(p =>
                p.CorrelationId == "corr-1" &&
                p.MessageId == "msg-1" &&
                p.ContentType == "application/vnd.test+json" &&
                !p.Persistent &&
                p.Headers!.ContainsKey("source")),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
