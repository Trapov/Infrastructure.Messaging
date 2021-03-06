﻿namespace Infrastructure.Messaging.Tests
{
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Infrastructure.Messaging.InMemory.Extensions;
    using System.Text.Json;

    public sealed class InMemoryTests
    {
        public sealed class TestMessage : IMessage
        {
            public string Ping { get; set; } = "Ping";
        }
        public sealed class TestMessageHandler : IMessageHandler<TestMessage>
        {
            public Task Handle(TestMessage message, CancellationToken cancellationToken)
            {
                message.Ping = "Pong";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void When_Published_Succsessfully_With_Json_Then_True()
        {
            var services = new ServiceCollection();

            services
                .AddLogging()
                .AddSingleton<BlockingCollection<(Type, object)>, BlockingCollection<(Type, object)>>()
                .AddMessaging(
                    mc => mc
                        .UseJsonPacker(jso => jso.PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
                        .UseInMemory(),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                );

            var serviceProvider = services.BuildServiceProvider();

            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

            var testMessage = new TestMessage();

            publisher.Publish(testMessage, CancellationToken.None).GetAwaiter().GetResult();

            var blockingCollection = serviceProvider.GetRequiredService<BlockingCollection<(Type, object)>>();

            var packedMessage = blockingCollection.Take().Item2;

            var message = JsonSerializer.Deserialize<TestMessage>((string)packedMessage);

            Assert.IsType<TestMessage>(message);
        }
    }
}
