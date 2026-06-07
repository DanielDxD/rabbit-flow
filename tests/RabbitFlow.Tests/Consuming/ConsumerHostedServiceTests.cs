using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitFlow.Consuming;
using RabbitFlow.Serialization;
using RabbitFlow.Tests.TestHelpers;
using RabbitFlow.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitFlow.Tests.Consuming;

public class ConsumerHostedServiceTests
{
    private readonly Capture<AsyncEventingBasicConsumer> _consumerCapture = new();

    [Fact]
    public async Task StartAsync_DoesNothing_WhenNoConsumersRegistered()
    {
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        var sut = CreateService(
            connectionProvider.Object,
            new ConsumerRegistrationCollection(),
            new TopologyRegistrationCollection());

        await sut.StartAsync(CancellationToken.None);

        connectionProvider.Verify(p => p.GetConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_StartsConsumerWorker()
    {
        var channelMock = CreateChannelMock();
        var connectionProvider = CreateConnectionProvider(channelMock);
        var registrations = CreateRegistration(autoAck: false);

        var services = new ServiceCollection()
            .AddSingleton(new TestConsumer())
            .BuildServiceProvider();

        var sut = CreateService(connectionProvider.Object, registrations, new TopologyRegistrationCollection(), services);

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        channelMock.Verify(c => c.BasicConsumeAsync(
            "test.queue",
            false,
            It.IsAny<string>(),
            false,
            false,
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<IAsyncBasicConsumer>(),
            It.IsAny<CancellationToken>()), Times.Once);
        channelMock.Verify(c => c.BasicCancelAsync("test-consumer", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consumer_AcksMessage_WhenProcessingSucceeds()
    {
        var channelMock = CreateChannelMock();
        var connectionProvider = CreateConnectionProvider(channelMock);
        var testConsumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton(testConsumer).BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        var sut = CreateService(
            connectionProvider.Object,
            CreateRegistration(autoAck: false),
            new TopologyRegistrationCollection(),
            services,
            serializer);

        await sut.StartAsync(CancellationToken.None);

        var body = serializer.Serialize(new TestMessage(Guid.NewGuid(), "Ack"));
        await ConsumerEventHelper.InvokeReceivedAsync(_consumerCapture.Value!, DeliveryEventFactory.Create(body, 42));

        Assert.Single(testConsumer.Received);
        channelMock.Verify(c => c.BasicAckAsync(42ul, false, It.IsAny<CancellationToken>()), Times.Once);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_NacksMessage_WhenProcessingFails()
    {
        var channelMock = CreateChannelMock();
        var connectionProvider = CreateConnectionProvider(channelMock);
        var testConsumer = new TestConsumer { ShouldThrow = true };
        var services = new ServiceCollection().AddSingleton(testConsumer).BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        var sut = CreateService(
            connectionProvider.Object,
            CreateRegistration(autoAck: false),
            new TopologyRegistrationCollection(),
            services,
            serializer);

        await sut.StartAsync(CancellationToken.None);

        var body = serializer.Serialize(new TestMessage(Guid.NewGuid(), "Nack"));
        await ConsumerEventHelper.InvokeReceivedAsync(_consumerCapture.Value!, DeliveryEventFactory.Create(body, 99));

        channelMock.Verify(c => c.BasicNackAsync(99ul, false, true, It.IsAny<CancellationToken>()), Times.Once);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_DoesNotAck_WhenAutoAckIsEnabled()
    {
        var channelMock = CreateChannelMock(autoAck: true);
        var connectionProvider = CreateConnectionProvider(channelMock);
        var testConsumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton(testConsumer).BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        var sut = CreateService(
            connectionProvider.Object,
            CreateRegistration(autoAck: true),
            new TopologyRegistrationCollection(),
            services,
            serializer);

        await sut.StartAsync(CancellationToken.None);

        var body = serializer.Serialize(new TestMessage(Guid.NewGuid(), "AutoAck"));
        await ConsumerEventHelper.InvokeReceivedAsync(_consumerCapture.Value!, DeliveryEventFactory.Create(body, 1));

        channelMock.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_DeclaresRegisteredTopologies()
    {
        var channelMock = CreateChannelMock();
        var connectionProvider = CreateConnectionProvider(channelMock);
        var topologyRegistrations = new TopologyRegistrationCollection();
        topologyRegistrations.Add(new ExchangeTopology(new PublishOptions { Exchange = "orders" }));

        var sut = CreateService(
            connectionProvider.Object,
            CreateRegistration(autoAck: false),
            topologyRegistrations);

        await sut.StartAsync(CancellationToken.None);

        channelMock.Verify(c => c.ExchangeDeclareAsync(
            "orders",
            ExchangeType.Topic,
            true,
            false,
            null,
            false,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private Mock<IChannel> CreateChannelMock(bool autoAck = false)
    {
        var (mock, _) = ChannelMockFactory.CreateConsumingChannel();
        mock.Setup(c => c.BasicConsumeAsync(
                It.IsAny<string>(),
                autoAck,
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
                (_, _, _, _, _, _, consumer, _) => _consumerCapture.Value = (AsyncEventingBasicConsumer)consumer)
            .ReturnsAsync("test-consumer");
        return mock;
    }

    private static ConsumerHostedService CreateService(
        IRabbitMqConnectionProvider connectionProvider,
        ConsumerRegistrationCollection registrations,
        TopologyRegistrationCollection topologyRegistrations,
        IServiceProvider? serviceProvider = null,
        IMessageSerializer? serializer = null) =>
        new(
            connectionProvider,
            serializer ?? new JsonMessageSerializer(),
            serviceProvider ?? new ServiceCollection().BuildServiceProvider(),
            registrations,
            topologyRegistrations,
            NullLogger<ConsumerHostedService>.Instance);

    private static Mock<IRabbitMqConnectionProvider> CreateConnectionProvider(Mock<IChannel> channelMock)
    {
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        connectionProvider.Setup(p => p.GetConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IConnection>());
        connectionProvider.Setup(p => p.CreateDedicatedChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);
        return connectionProvider;
    }

    private static ConsumerRegistrationCollection CreateRegistration(bool autoAck)
    {
        var registrations = new ConsumerRegistrationCollection();
        registrations.Add(new ConsumerRegistration
        {
            ConsumerType = typeof(TestConsumer),
            MessageType = typeof(TestMessage),
            Options = new ConsumeOptions
            {
                Queue = "test.queue",
                AutoAck = autoAck
            }
        });
        return registrations;
    }

    private sealed class Capture<T> where T : class
    {
        public T? Value { get; set; }
    }
}
