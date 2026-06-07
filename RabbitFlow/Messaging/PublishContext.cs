namespace RabbitFlow.Messaging;

public sealed class PublishContext
{
    public string? CorrelationId { get; init; }
    public string? MessageId { get; init; }
    public string? ContentType { get; init; }
    public IDictionary<string, object?>? Headers { get; init; }
    public bool Persistent { get; init; } = true;
    public bool Mandatory { get; init; }
}
