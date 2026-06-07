namespace RabbitFlow.Configuration;

public sealed class RabbitFlowOptions
{
    public const string SectionName = "RabbitFlow";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ClientProvidedName { get; set; } = "RabbitFlow";
    public int ChannelPoolSize { get; set; } = 16;
    public TimeSpan ConnectionRecoveryInterval { get; set; } = TimeSpan.FromSeconds(10);
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public bool TopologyRecoveryEnabled { get; set; } = true;
}
