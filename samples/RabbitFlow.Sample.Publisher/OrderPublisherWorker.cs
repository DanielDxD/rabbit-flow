using RabbitFlow.Abstractions;
using RabbitFlow.Samples.Contracts;

namespace RabbitFlow.Sample.Publisher;

public sealed class OrderPublisherWorker : BackgroundService
{
    private readonly ITypedPublisher<OrderCreatedEvent> _publisher;
    private readonly ILogger<OrderPublisherWorker> _logger;

    public OrderPublisherWorker(
        ITypedPublisher<OrderCreatedEvent> publisher,
        ILogger<OrderPublisherWorker> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Publisher started. Sending an order every 3 seconds...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var order = new OrderCreatedEvent(
                OrderId: Guid.NewGuid(),
                CustomerName: "Jane Doe",
                Total: Random.Shared.Next(50, 500));

            await _publisher.PublishAsync(order, cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Published order {OrderId} for {CustomerName} — total {Total:C}",
                order.OrderId,
                order.CustomerName,
                order.Total);

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
