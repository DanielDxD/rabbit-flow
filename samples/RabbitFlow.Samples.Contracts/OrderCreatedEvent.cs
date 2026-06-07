namespace RabbitFlow.Samples.Contracts;

public sealed record OrderCreatedEvent(Guid OrderId, string CustomerName, decimal Total);
