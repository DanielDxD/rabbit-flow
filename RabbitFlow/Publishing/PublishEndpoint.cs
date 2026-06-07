using RabbitFlow.Configuration;

namespace RabbitFlow.Publishing;

internal sealed class PublishEndpoint<TMessage>
{
    public PublishEndpoint(PublishOptions options)
    {
        Options = options;
    }

    public PublishOptions Options { get; }
}
