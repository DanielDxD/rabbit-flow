using RabbitFlow.DependencyInjection;
using RabbitFlow.Sample.Publisher;
using RabbitFlow.Samples.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitFlow(options =>
{
    options.HostName = builder.Configuration["RabbitFlow:HostName"] ?? "localhost";
    options.Port = builder.Configuration.GetValue("RabbitFlow:Port", 5672);
    options.UserName = builder.Configuration["RabbitFlow:UserName"] ?? "guest";
    options.Password = builder.Configuration["RabbitFlow:Password"] ?? "guest";
    options.VirtualHost = builder.Configuration["RabbitFlow:VirtualHost"] ?? "/";
    options.ClientProvidedName = "rabbitflow-sample-publisher";
})
.AddPublisher<OrderCreatedEvent>(publish =>
{
    publish.Exchange = "orders";
    publish.RoutingKey = "order.created";
    publish.Persistent = true;
});

builder.Services.AddHostedService<OrderPublisherWorker>();

var host = builder.Build();
await host.RunAsync();
