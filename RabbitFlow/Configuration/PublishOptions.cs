namespace RabbitFlow.Configuration;

public sealed class PublishOptions
{
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public bool Mandatory { get; set; }
    public bool Persistent { get; set; } = true;
}
