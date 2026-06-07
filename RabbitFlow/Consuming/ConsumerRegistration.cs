using RabbitFlow.Configuration;

namespace RabbitFlow.Consuming;

internal sealed class ConsumerRegistration
{
    public required Type ConsumerType { get; init; }
    public required Type MessageType { get; init; }
    public required ConsumeOptions Options { get; init; }
}

internal sealed class ConsumerRegistrationCollection
{
    private readonly List<ConsumerRegistration> _registrations = [];

    public IReadOnlyList<ConsumerRegistration> Registrations => _registrations;

    public void Add(ConsumerRegistration registration) => _registrations.Add(registration);
}
