using Microsoft.Extensions.DependencyInjection;
using RabbitFlow.Tests.TestHelpers;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitFlow.Consuming;
using RabbitFlow.DependencyInjection;
using RabbitFlow.Serialization;
using RabbitFlow.Topology;

namespace RabbitFlow.Tests.DependencyInjection;

public class RabbitFlowBuilderTests
{
    [Fact]
    public void AddConsumer_RegistersConsumerTopologyAndRegistration()
    {
        var services = ServiceCollectionHelper.Create();
        services.AddRabbitFlow()
            .AddConsumer<TestConsumer, TestMessage>(consume =>
            {
                consume.Queue = "test.queue";
                consume.Exchange = "test.exchange";
                consume.BindingRoutingKey = "test.key";
            });

        var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<ConsumerRegistrationCollection>();
        var topologies = provider.GetRequiredService<TopologyRegistrationCollection>().ResolveAll(provider);

        Assert.Single(registrations.Registrations);
        Assert.Equal(typeof(TestConsumer), registrations.Registrations[0].ConsumerType);
        Assert.Equal(3, topologies.Count);
        Assert.NotNull(provider.GetService<TestConsumer>());
    }

    [Fact]
    public void AddConsumer_ThrowsWhenQueueIsMissing()
    {
        var services = ServiceCollectionHelper.Create();
        var builder = services.AddRabbitFlow();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.AddConsumer<TestConsumer, TestMessage>());

        Assert.Contains("Queue name is required", ex.Message);
    }

    [Fact]
    public void AddPublisher_RegistersTypedPublisherAndTopology()
    {
        var services = ServiceCollectionHelper.Create();
        services.AddRabbitFlow()
            .AddPublisher<TestMessage>(publish =>
            {
                publish.Exchange = "orders";
                publish.RoutingKey = "order.created";
            });

        var provider = services.BuildServiceProvider();
        var topologies = provider.GetRequiredService<TopologyRegistrationCollection>().ResolveAll(provider);

        Assert.NotNull(provider.GetService<ITypedPublisher<TestMessage>>());
        Assert.Single(topologies);
        Assert.IsType<ExchangeTopology>(topologies[0]);
    }

    [Fact]
    public void AddSerializer_ReplacesDefaultSerializer()
    {
        var services = ServiceCollectionHelper.Create();
        services.AddRabbitFlow()
            .AddSerializer<CustomSerializer>();

        var provider = services.BuildServiceProvider();

        Assert.IsType<CustomSerializer>(provider.GetRequiredService<IMessageSerializer>());
    }

    [Fact]
    public void AddTopology_RegistersCustomTopology()
    {
        var services = ServiceCollectionHelper.Create();
        services.AddRabbitFlow()
            .AddTopology<CustomTopology>();

        var provider = services.BuildServiceProvider();
        var topologies = provider.GetRequiredService<TopologyRegistrationCollection>().ResolveAll(provider);

        Assert.Single(topologies);
        Assert.IsType<CustomTopology>(topologies[0]);
    }

    [Fact]
    public void Configure_AppliesOptions()
    {
        var services = ServiceCollectionHelper.Create();
        services.AddRabbitFlow()
            .Configure(options => options.ChannelPoolSize = 32);

        var provider = services.BuildServiceProvider();

        Assert.Equal(32, provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitFlowOptions>>().Value.ChannelPoolSize);
    }

    private sealed class CustomSerializer : IMessageSerializer
    {
        public ReadOnlyMemory<byte> Serialize<T>(T message) => ReadOnlyMemory<byte>.Empty;
        public T Deserialize<T>(ReadOnlySpan<byte> body) => default!;
        public object Deserialize(Type messageType, ReadOnlySpan<byte> body) => new object();
    }

    private sealed class CustomTopology : IRabbitMqTopology
    {
        public Task DeclareAsync(RabbitMQ.Client.IChannel channel, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
