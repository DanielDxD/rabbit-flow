using Microsoft.Extensions.DependencyInjection;
using RabbitFlow.Tests.TestHelpers;
using Microsoft.Extensions.Hosting;
using RabbitFlow.Abstractions;
using RabbitFlow.Configuration;
using RabbitFlow.DependencyInjection;
using RabbitFlow.Serialization;

namespace RabbitFlow.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRabbitFlow_RegistersCoreServices()
    {
        var services = ServiceCollectionHelper.Create();

        services.AddRabbitFlow(options => options.HostName = "rabbit");

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IRabbitMqConnectionProvider>());
        Assert.NotNull(provider.GetService<IMessageSerializer>());
        Assert.NotNull(provider.GetService<IPublisher>());
        Assert.Equal("rabbit", provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitFlowOptions>>().Value.HostName);
        Assert.Equal(2, provider.GetServices<IHostedService>().Count());
    }

    [Fact]
    public void AddRabbitFlow_ThrowsWhenServicesIsNull()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddRabbitFlow());
    }
}
