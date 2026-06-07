using Moq;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitFlow.Messaging;
using RabbitFlow.Publishing;
using RabbitFlow.Tests.TestHelpers;

namespace RabbitFlow.Tests.Publishing;

public class TypedPublisherTests
{
    [Fact]
    public async Task PublishAsync_UsesEndpointOptions()
    {
        var publisher = new Mock<IPublisher>();
        var message = new TestMessage(Guid.NewGuid(), "Typed");
        var endpoint = new PublishEndpoint<TestMessage>(new PublishOptions
        {
            Exchange = "orders",
            RoutingKey = "order.created",
            Mandatory = true,
            Persistent = false
        });

        publisher.Setup(p => p.PublishAsync(
                message,
                "orders",
                "order.created",
                It.Is<PublishContext>(c => c.Mandatory && !c.Persistent),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new TypedPublisher<TestMessage>(publisher.Object, endpoint);

        await sut.PublishAsync(message);

        publisher.VerifyAll();
    }

    [Fact]
    public async Task PublishAsync_PreservesProvidedContext()
    {
        var publisher = new Mock<IPublisher>();
        var message = new TestMessage(Guid.NewGuid(), "Typed");
        var endpoint = new PublishEndpoint<TestMessage>(new PublishOptions
        {
            Exchange = "orders",
            RoutingKey = "order.created"
        });
        var context = new PublishContext { CorrelationId = "abc" };

        publisher.Setup(p => p.PublishAsync(
                message,
                "orders",
                "order.created",
                context,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new TypedPublisher<TestMessage>(publisher.Object, endpoint);

        await sut.PublishAsync(message, context);

        publisher.VerifyAll();
    }
}
