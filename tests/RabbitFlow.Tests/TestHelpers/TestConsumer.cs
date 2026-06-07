using RabbitFlow.Messaging;

namespace RabbitFlow.Tests.TestHelpers;

public sealed class TestConsumer : Consumer<TestMessage>
{
    public List<TestMessage> Received { get; } = [];
    public bool ShouldThrow { get; set; }

    public override Task ConsumeAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Consumer failure");
        }

        Received.Add(message);
        return Task.CompletedTask;
    }
}

public sealed class InvalidTestConsumer
{
    public Task ConsumeAsync(TestMessage message) => Task.CompletedTask;
}
