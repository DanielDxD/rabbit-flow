using System.Text.Json;
using RabbitFlow.Abstractions;

namespace RabbitFlow.Serialization;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly JsonSerializerOptions _options;

    public JsonMessageSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? DefaultOptions;
    }

    public ReadOnlyMemory<byte> Serialize<T>(T message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, _options);

    public T Deserialize<T>(ReadOnlySpan<byte> body) =>
        JsonSerializer.Deserialize<T>(body, _options)
        ?? throw new InvalidOperationException($"Failed to deserialize message to {typeof(T).Name}.");

    public object Deserialize(Type messageType, ReadOnlySpan<byte> body)
    {
        var message = JsonSerializer.Deserialize(body, messageType, _options);
        return message ?? throw new InvalidOperationException($"Failed to deserialize message to {messageType.Name}.");
    }
}
