using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitFlow.Connection;
using RabbitFlow.Consuming;
using RabbitFlow.Publishing;
using RabbitFlow.Serialization;

namespace RabbitFlow.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static RabbitFlowBuilder AddRabbitFlow(
        this IServiceCollection services,
        Action<RabbitFlowOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<RabbitFlowOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.TryAddSingleton<IPublisher, RabbitMqPublisher>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConsumerHostedService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RabbitMqConnectionLifetime>());

        return new RabbitFlowBuilder(services);
    }
}
