# Infrastructure.Messaging

Library for messaging between services.

# Builds
||Travis|
|--------|--------|
|Infrastructure.Messaging|[![Build Status](https://travis-ci.com/Trapov/Infrastructure.Messaging.svg?branch=master)](https://travis-ci.com/Trapov/Infrastructure.Messaging)|

# Requirements
- Microsoft.Extensions.DependencyInjection (>= 2.2.0)
- Microsoft.Extensions.Logging.Abstractions (>= 2.2.0)
- Newtonsoft.Json (>= 12.0.2)
- System.Interactive.Async (>= 4.0.0-preview.8.build.9)

# Installation

|Packages|Travis|
|--------|--------|
|Infrastructure.Messaging|[![NuGet version](https://badge.fury.io/nu/Common.Infrastructure.Messaging.png)](https://badge.fury.io/nu/Common.Infrastructure.Messaging)|
|Infrastructure.Messaging.DependencyInjection|[![NuGet version](https://badge.fury.io/nu/Common.Infrastructure.Messaging.Extensions.DependencyInjection.png)](https://badge.fury.io/nu/Common.Infrastructure.Messaging.Extensions.DependencyInjection.png)|
|Infrastructure.Messaging.RabbitMQ|[![NuGet version](https://badge.fury.io/nu/Common.Infrastructure.Messaging.RabbitMQ.png)](https://badge.fury.io/nu/Common.Infrastructure.Messaging.RabbitMQ)|
|Infrastructure.Messaging.InMemory|[![NuGet version](https://badge.fury.io/nu/Common.Infrastructure.Messaging.InMemory.png)](https://badge.fury.io/nu/Common.Infrastructure.Messaging.InMemory)|




# How to

Register Messaging via `.AddMessaging()` extension method provided by `Infrastructure.Messaging.Extensions.DependencyInjection` package.
```cs

var rabbitMQUri = configuration.GetSection("RabbitMq")["uri"];

var serviceProvider = 
    new ServiceCollection()
        .AddLogging(lb => lb.AddConsole())
        .AddMessaging(mc =>
            {
                mc.UseRabbitMQ(cf => cf.Uri = new Uri(rabbitMQUri));
                mc.UseJsonPacker(jss => jss.ContractResolver = new CamelCasePropertyNamesContractResolver());
            }, 
            sc => sc
                .AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                .AddSingleton<IMessageHandler<TestMessageWithEventId>, TestMessageHandler>()
        )
    .BuildServiceProvider();
```

Implement handlers.

```cs
public sealed class TestMessageHandler : 
    IMessageHandler<TestMessage>,
    IMessageHandler<TestMessageWithEventId>
{
    private readonly ILogger<TestMessageHandler> _logger;

    public TestMessageHandler(ILogger<TestMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TestMessage message, CancellationToken cancellationToken)
    {
        await Task.Delay(100);
        //_logger.LogInformation("Test message was dispached {message}", message.Ping);
        throw new Exception("AASDSD");
    }

    public Task Handle(TestMessageWithEventId message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("TestMessageWithEventId was dispatched. {eventId}, {text}", message.EventId, message.Text);
        return Task.CompletedTask;
    }
}
```