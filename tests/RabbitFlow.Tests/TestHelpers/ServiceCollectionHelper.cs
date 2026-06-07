using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RabbitFlow.Tests.TestHelpers;

internal static class ServiceCollectionHelper
{
    public static IServiceCollection Create() =>
        new ServiceCollection().AddLogging();
}
