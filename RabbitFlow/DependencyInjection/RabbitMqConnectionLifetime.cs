using Microsoft.Extensions.Hosting;
using RabbitFlow.Abstractions;

namespace RabbitFlow.DependencyInjection;

internal sealed class RabbitMqConnectionLifetime : IHostedService
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;

    public RabbitMqConnectionLifetime(IRabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        _connectionProvider.GetConnectionAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connectionProvider is IAsyncDisposable disposable)
        {
            return disposable.DisposeAsync().AsTask();
        }

        return Task.CompletedTask;
    }
}
