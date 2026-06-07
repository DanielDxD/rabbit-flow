using RabbitFlow.Messaging;
using RabbitFlow.Tests.TestHelpers;

namespace RabbitFlow.Tests.Messaging;

public class MessagingTests
{
    [Fact]
    public async Task ConsumerBase_InvokesConsumeAsync()
    {
        var consumer = new TestConsumer();
        var message = new TestMessage(Guid.NewGuid(), "Test");

        await consumer.ConsumeAsync(message);

        Assert.Single(consumer.Received);
    }

    [Fact]
    public void PublishContext_HasExpectedDefaults()
    {
        var context = new PublishContext();

        Assert.Null(context.CorrelationId);
        Assert.Null(context.MessageId);
        Assert.Null(context.ContentType);
        Assert.Null(context.Headers);
        Assert.True(context.Persistent);
        Assert.False(context.Mandatory);
    }
}
