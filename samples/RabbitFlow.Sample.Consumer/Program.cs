using RabbitFlow.DependencyInjection;
using RabbitFlow.Sample.Consumer;
using RabbitFlow.Samples.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitFlow(options =>
{
    options.HostName = builder.Configuration["RabbitFlow:HostName"] ?? "localhost";
    options.Port = builder.Configuration.GetValue("RabbitFlow:Port", 5672);
    options.UserName = builder.Configuration["RabbitFlow:UserName"] ?? "guest";
    options.Password = builder.Configuration["RabbitFlow:Password"] ?? "guest";
    options.VirtualHost = builder.Configuration["RabbitFlow:VirtualHost"] ?? "/";
    options.ClientProvidedName = "rabbitflow-sample-consumer";
})
.AddConsumer<OrderCreatedConsumer, OrderCreatedEvent>(consume =>
{
    consume.Queue = "orders.created";
    consume.Exchange = "orders";
    consume.BindingRoutingKey = "order.created";
    consume.PrefetchCount = 10;
    consume.DurableQueue = true;
});

var host = builder.Build();
await host.RunAsync();
