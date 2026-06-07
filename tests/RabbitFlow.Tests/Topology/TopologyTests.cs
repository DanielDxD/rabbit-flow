using Moq;
using RabbitFlow.Configuration;
using RabbitFlow.Topology;
using RabbitMQ.Client;

namespace RabbitFlow.Tests.Topology;

public class TopologyTests
{
    [Fact]
    public async Task ExchangeTopology_SkipsDeclaration_WhenExchangeIsEmpty()
    {
        var channel = new Mock<IChannel>();
        var sut = new ExchangeTopology(new PublishOptions { Exchange = "  " });

        await sut.DeclareAsync(channel.Object);

        channel.Verify(
            c => c.ExchangeDeclareAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExchangeTopology_DeclaresExchange_WhenConfigured()
    {
        var channel = new Mock<IChannel>();
        channel.Setup(c => c.ExchangeDeclareAsync(
                "orders",
                ExchangeType.Topic,
                true,
                false,
                null,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ExchangeTopology(new PublishOptions { Exchange = "orders" });

        await sut.DeclareAsync(channel.Object);

        channel.VerifyAll();
    }

    [Fact]
    public async Task QueueTopology_DeclaresQueueWithOptions()
    {
        var channel = new Mock<IChannel>();
        channel.Setup(c => c.QueueDeclareAsync(
                "orders.created",
                true,
                false,
                false,
                null,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueDeclareOk("orders.created", 1, 2));

        var sut = new QueueTopology(new ConsumeOptions
        {
            Queue = "orders.created",
            DurableQueue = true
        });

        await sut.DeclareAsync(channel.Object);

        channel.VerifyAll();
    }

    [Fact]
    public async Task QueueBindingTopology_SkipsBinding_WhenExchangeIsEmpty()
    {
        var channel = new Mock<IChannel>();
        var sut = new QueueBindingTopology(new ConsumeOptions
        {
            Queue = "orders.created",
            Exchange = null
        });

        await sut.DeclareAsync(channel.Object);

        channel.Verify(
            c => c.QueueBindAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task QueueBindingTopology_BindsQueueToExchange()
    {
        var channel = new Mock<IChannel>();
        channel.Setup(c => c.QueueBindAsync(
                "orders.created",
                "orders",
                "order.created",
                null,
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new QueueBindingTopology(new ConsumeOptions
        {
            Queue = "orders.created",
            Exchange = "orders",
            BindingRoutingKey = "order.created"
        });

        await sut.DeclareAsync(channel.Object);

        channel.VerifyAll();
    }
}
