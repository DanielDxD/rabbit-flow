using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitFlow.Abstractions;
using RabbitFlow.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitFlow.Consuming;

internal sealed class ConsumerHostedService(
    IRabbitMqConnectionProvider connectionProvider,
    IMessageSerializer serializer,
    IServiceProvider serviceProvider,
    ConsumerRegistrationCollection registrations,
    TopologyRegistrationCollection topologyRegistrations,
    ILogger<ConsumerHostedService> logger)
    : IHostedService
{
    private readonly List<ConsumerWorker> _workers = [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (registrations.Registrations.Count == 0)
        {
            return;
        }

        await connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);

        var topologies = topologyRegistrations.ResolveAll(serviceProvider);

        foreach (var registration in registrations.Registrations)
        {
            var worker = await ConsumerWorker.CreateAsync(
                registration,
                connectionProvider,
                serializer,
                serviceProvider,
                topologies,
                logger,
                cancellationToken).ConfigureAwait(false);

            _workers.Add(worker);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var worker in _workers)
        {
            await worker.DisposeAsync().ConfigureAwait(false);
        }

        _workers.Clear();
    }

    private sealed class ConsumerWorker : IAsyncDisposable
    {
        private readonly IChannel _channel;
        private readonly string _consumerTag;

        private ConsumerWorker(IChannel channel, string consumerTag)
        {
            _channel = channel;
            _consumerTag = consumerTag;
        }

        public static async Task<ConsumerWorker> CreateAsync(
            ConsumerRegistration registration,
            IRabbitMqConnectionProvider connectionProvider,
            IMessageSerializer serializer,
            IServiceProvider serviceProvider,
            IEnumerable<IRabbitMqTopology> topologies,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var channel = await connectionProvider.CreateDedicatedChannelAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var topology in topologies)
            {
                await topology.DeclareAsync(channel, cancellationToken).ConfigureAwait(false);
            }

            var options = registration.Options;
            await channel.QueueDeclareAsync(
                queue: options.Queue,
                durable: options.DurableQueue,
                exclusive: options.ExclusiveQueue,
                autoDelete: options.AutoDeleteQueue,
                arguments: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: options.PrefetchCount,
                global: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                await HandleMessageAsync(
                    registration,
                    serializer,
                    serviceProvider,
                    channel,
                    eventArgs,
                    options.AutoAck,
                    logger).ConfigureAwait(false);
            };

            var consumerTag = await channel.BasicConsumeAsync(
                queue: options.Queue,
                autoAck: options.AutoAck,
                consumer: consumer,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Consumer {ConsumerType} started on queue '{Queue}'",
                registration.ConsumerType.Name,
                options.Queue);

            return new ConsumerWorker(channel, consumerTag);
        }

        private static async Task HandleMessageAsync(
            ConsumerRegistration registration,
            IMessageSerializer serializer,
            IServiceProvider serviceProvider,
            IChannel channel,
            BasicDeliverEventArgs eventArgs,
            bool autoAck,
            ILogger logger)
        {
            try
            {
                var message = serializer.Deserialize(registration.MessageType, eventArgs.Body.Span);

                await using var scope = serviceProvider.CreateAsyncScope();
                var consumer = scope.ServiceProvider.GetRequiredService(registration.ConsumerType);
                var consumeMethod = registration.ConsumerType.GetMethod(
                    nameof(IConsumer<object>.ConsumeAsync),
                    [registration.MessageType, typeof(CancellationToken)])
                    ?? throw new InvalidOperationException(
                        $"Consumer {registration.ConsumerType.Name} does not implement ConsumeAsync.");

                var task = (Task)consumeMethod.Invoke(consumer, [message, CancellationToken.None])!;
                await task.ConfigureAwait(false);

                if (!autoAck)
                {
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error consuming message from queue '{Queue}' with consumer {ConsumerType}",
                    registration.Options.Queue,
                    registration.ConsumerType.Name);

                if (!autoAck)
                {
                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: true).ConfigureAwait(false);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel.IsOpen)
            {
                await _channel.BasicCancelAsync(_consumerTag).ConfigureAwait(false);
                await _channel.CloseAsync().ConfigureAwait(false);
            }

            await _channel.DisposeAsync().ConfigureAwait(false);
        }
    }
}
