using Moq;
using RabbitFlow.Abstractions;
using RabbitFlow.DependencyInjection;

namespace RabbitFlow.Tests.DependencyInjection;

public class RabbitMqConnectionLifetimeTests
{
    [Fact]
    public async Task StartAsync_EstablishesConnection()
    {
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        connectionProvider.Setup(p => p.GetConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<RabbitMQ.Client.IConnection>());

        var sut = new RabbitMqConnectionLifetime(connectionProvider.Object);

        await sut.StartAsync(CancellationToken.None);

        connectionProvider.Verify(p => p.GetConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DisposesProviderWhenAsyncDisposable()
    {
        var disposable = new Mock<IRabbitMqConnectionProvider>();
        disposable.As<IAsyncDisposable>()
            .Setup(d => d.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var sut = new RabbitMqConnectionLifetime(disposable.Object);

        await sut.StopAsync(CancellationToken.None);

        disposable.As<IAsyncDisposable>().Verify(d => d.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DoesNothingWhenProviderIsNotAsyncDisposable()
    {
        var connectionProvider = new Mock<IRabbitMqConnectionProvider>();
        var sut = new RabbitMqConnectionLifetime(connectionProvider.Object);

        await sut.StopAsync(CancellationToken.None);
    }
}
