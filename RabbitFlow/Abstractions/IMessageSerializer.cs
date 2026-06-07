namespace RabbitFlow.Abstractions;

public interface IMessageSerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T message);
    T Deserialize<T>(ReadOnlySpan<byte> body);
    object Deserialize(Type messageType, ReadOnlySpan<byte> body);
}
