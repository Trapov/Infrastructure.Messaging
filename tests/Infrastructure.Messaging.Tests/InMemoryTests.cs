namespace Infrastructure.Messaging.Tests
{
    using Infrastructure.Messaging.InMemory;
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Infrastructure.Messaging.InMemory.Extensions;

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
                        .UseJsonPacker(jc => { })
                        .UseInMemory(),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                );

            var serviceProvider = services.BuildServiceProvider();

            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

            var testMessage = new TestMessage();

            publisher.Publish(testMessage, CancellationToken.None).GetAwaiter().GetResult();

            var blockingCollection = serviceProvider.GetRequiredService<BlockingCollection<(Type, object)>>();

            var message = JsonConvert.DeserializeObject<TestMessage>((string)blockingCollection.Take().Item2);

            Assert.IsType<TestMessage>(message);
        }
    }
}
