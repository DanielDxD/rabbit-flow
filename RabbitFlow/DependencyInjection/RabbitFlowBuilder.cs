using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitFlow.Consuming;
using RabbitFlow.Messaging;
using RabbitFlow.Topology;

namespace RabbitFlow.DependencyInjection;

public sealed class RabbitFlowBuilder
{
    private readonly IServiceCollection _services;
    private readonly ConsumerRegistrationCollection _consumerRegistrations = new();
    private readonly TopologyRegistrationCollection _topologyRegistrations = new();

    internal RabbitFlowBuilder(IServiceCollection services)
    {
        _services = services;
        _services.TryAddSingleton(_consumerRegistrations);
        _services.TryAddSingleton(_topologyRegistrations);
    }

    public RabbitFlowBuilder Configure(Action<RabbitFlowOptions> configure)
    {
        _services.Configure(configure);
        return this;
    }

    public RabbitFlowBuilder AddSerializer<TSerializer>()
        where TSerializer : class, IMessageSerializer
    {
        _services.Replace(ServiceDescriptor.Singleton<IMessageSerializer, TSerializer>());
        return this;
    }

    public RabbitFlowBuilder AddTopology<TTopology>()
        where TTopology : class, IRabbitMqTopology
    {
        _services.TryAddSingleton<TTopology>();
        _topologyRegistrations.Add<TTopology>();
        return this;
    }

    public RabbitFlowBuilder AddPublisher<TMessage>(
        Action<PublishOptions>? configure = null)
    {
        var options = new PublishOptions();
        configure?.Invoke(options);

        _topologyRegistrations.Add(new ExchangeTopology(options));

        _services.AddSingleton(new Publishing.PublishEndpoint<TMessage>(options));
        _services.TryAddSingleton<Abstractions.ITypedPublisher<TMessage>, Publishing.TypedPublisher<TMessage>>();
        _services.TryAddSingleton<IPublisher, Publishing.RabbitMqPublisher>();
        return this;
    }

    public RabbitFlowBuilder AddConsumer<TConsumer, TMessage>(
        Action<ConsumeOptions>? configure = null)
        where TConsumer : class, IConsumer<TMessage>
    {
        var options = new ConsumeOptions();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.Queue))
        {
            throw new InvalidOperationException(
                $"Queue name is required for consumer {typeof(TConsumer).Name}.");
        }

        _services.TryAddSingleton<TConsumer>();
        _topologyRegistrations.Add(new QueueTopology(options));

        if (!string.IsNullOrWhiteSpace(options.Exchange))
        {
            _topologyRegistrations.Add(new ExchangeTopology(new PublishOptions { Exchange = options.Exchange }));
            _topologyRegistrations.Add(new QueueBindingTopology(options));
        }

        _consumerRegistrations.Add(new ConsumerRegistration
        {
            ConsumerType = typeof(TConsumer),
            MessageType = typeof(TMessage),
            Options = options
        });

        return this;
    }
}
