namespace RabbitFlow.Configuration;

public sealed class ConsumeOptions
{
    public string Queue { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public string? BindingRoutingKey { get; set; }
    public ushort PrefetchCount { get; set; } = 10;
    public bool AutoAck { get; set; }
    public bool DurableQueue { get; set; } = true;
    public bool ExclusiveQueue { get; set; }
    public bool AutoDeleteQueue { get; set; }
}
