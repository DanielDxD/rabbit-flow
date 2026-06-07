using RabbitFlow.Abstractions;
using RabbitFlow.Messaging;

namespace RabbitFlow.Publishing;

internal sealed class TypedPublisher<TMessage> : ITypedPublisher<TMessage>
{
    private readonly IPublisher _publisher;
    private readonly PublishEndpoint<TMessage> _endpoint;

    public TypedPublisher(IPublisher publisher, PublishEndpoint<TMessage> endpoint)
    {
        _publisher = publisher;
        _endpoint = endpoint;
    }

    public Task PublishAsync(
        TMessage message,
        PublishContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var options = _endpoint.Options;
        context ??= new PublishContext
        {
            Mandatory = options.Mandatory,
            Persistent = options.Persistent
        };

        return _publisher.PublishAsync(
            message,
            options.Exchange,
            options.RoutingKey,
            context,
            cancellationToken);
    }
}
