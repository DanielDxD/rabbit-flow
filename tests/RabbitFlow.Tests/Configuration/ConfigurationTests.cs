using RabbitFlow.Configuration;

namespace RabbitFlow.Tests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void RabbitFlowOptions_HasExpectedDefaults()
    {
        var options = new RabbitFlowOptions();

        Assert.Equal("localhost", options.HostName);
        Assert.Equal(5672, options.Port);
        Assert.Equal("guest", options.UserName);
        Assert.Equal("guest", options.Password);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal("RabbitFlow", options.ClientProvidedName);
        Assert.Equal(16, options.ChannelPoolSize);
        Assert.True(options.AutomaticRecoveryEnabled);
        Assert.True(options.TopologyRecoveryEnabled);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ConnectionRecoveryInterval);
        Assert.Equal("RabbitFlow", RabbitFlowOptions.SectionName);
    }

    [Fact]
    public void PublishOptions_HasExpectedDefaults()
    {
        var options = new PublishOptions();

        Assert.Equal(string.Empty, options.Exchange);
        Assert.Equal(string.Empty, options.RoutingKey);
        Assert.False(options.Mandatory);
        Assert.True(options.Persistent);
    }

    [Fact]
    public void ConsumeOptions_HasExpectedDefaults()
    {
        var options = new ConsumeOptions();

        Assert.Equal(string.Empty, options.Queue);
        Assert.Null(options.Exchange);
        Assert.Null(options.BindingRoutingKey);
        Assert.Equal((ushort)10, options.PrefetchCount);
        Assert.False(options.AutoAck);
        Assert.True(options.DurableQueue);
        Assert.False(options.ExclusiveQueue);
        Assert.False(options.AutoDeleteQueue);
    }
}
