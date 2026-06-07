using System.Text;
using RabbitFlow.Serialization;
using RabbitFlow.Tests.TestHelpers;

namespace RabbitFlow.Tests.Serialization;

public class JsonMessageSerializerTests
{
    private readonly JsonMessageSerializer _sut = new();

    [Fact]
    public void Serialize_ProducesCamelCaseJson()
    {
        var message = new TestMessage(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), "Alice");

        var bytes = _sut.Serialize(message);

        Assert.Equal(
            """{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","name":"Alice"}""",
            Encoding.UTF8.GetString(bytes.Span));
    }

    [Fact]
    public void Deserialize_Generic_RoundTripsMessage()
    {
        var json = """{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","name":"Bob"}"""u8.ToArray();
        var expected = new TestMessage(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), "Bob");

        var result = _sut.Deserialize<TestMessage>(json);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Deserialize_NonGeneric_RoundTripsMessage()
    {
        var json = """{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","name":"Carol"}"""u8.ToArray();

        var result = (TestMessage)_sut.Deserialize(typeof(TestMessage), json);

        Assert.Equal("Carol", result.Name);
    }

    [Fact]
    public void Deserialize_Generic_ThrowsWhenPayloadIsInvalid()
    {
        var json = """{"id":"not-a-guid","name":123}"""u8.ToArray();

        Assert.ThrowsAny<Exception>(() => _sut.Deserialize<TestMessage>(json));
    }

    [Fact]
    public void Deserialize_NonGeneric_ThrowsWhenPayloadIsNullLiteral()
    {
        var json = "null"u8.ToArray();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _sut.Deserialize(typeof(TestMessage), json));

        Assert.Contains("TestMessage", ex.Message);
    }
}
