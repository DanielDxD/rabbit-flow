using Microsoft.Extensions.DependencyInjection;
using RabbitFlow.Abstractions;

namespace RabbitFlow.Topology;

internal sealed class TopologyRegistrationCollection
{
    private readonly List<Func<IServiceProvider, IRabbitMqTopology>> _factories = [];

    public void Add(IRabbitMqTopology topology) => _factories.Add(_ => topology);

    public void Add<TTopology>()
        where TTopology : class, IRabbitMqTopology =>
        _factories.Add(static sp => sp.GetRequiredService<TTopology>());

    public IReadOnlyList<IRabbitMqTopology> ResolveAll(IServiceProvider serviceProvider) =>
        _factories.Select(factory => factory(serviceProvider)).ToList();
}
