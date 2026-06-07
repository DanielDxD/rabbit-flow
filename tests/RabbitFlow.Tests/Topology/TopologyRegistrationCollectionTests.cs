using Microsoft.Extensions.DependencyInjection;
using RabbitFlow.Abstractions;
using RabbitFlow.Topology;
using RabbitMQ.Client;

namespace RabbitFlow.Tests.Topology;

public class TopologyRegistrationCollectionTests
{
    [Fact]
    public void ResolveAll_ReturnsRegisteredTopologies()
    {
        var sut = new TopologyRegistrationCollection();
        var exchangeTopology = new ExchangeTopology(new RabbitFlow.Configuration.PublishOptions { Exchange = "orders" });
        sut.Add(exchangeTopology);

        var services = new ServiceCollection().BuildServiceProvider();
        var resolved = sut.ResolveAll(services);

        Assert.Single(resolved);
        Assert.Same(exchangeTopology, resolved[0]);
    }

    [Fact]
    public void AddGeneric_ResolvesTopologyFromServiceProvider()
    {
        var sut = new TopologyRegistrationCollection();
        var custom = new CustomTopology();
        var services = new ServiceCollection()
            .AddSingleton<IRabbitMqTopology>(custom)
            .AddSingleton(custom)
            .BuildServiceProvider();

        sut.Add<CustomTopology>();

        var resolved = sut.ResolveAll(services);

        Assert.Single(resolved);
        Assert.IsType<CustomTopology>(resolved[0]);
    }

    private sealed class CustomTopology : IRabbitMqTopology
    {
        public Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
