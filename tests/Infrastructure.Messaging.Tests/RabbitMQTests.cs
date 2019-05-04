namespace Infrastructure.Messaging.Tests
{
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Infrastructure.Messaging.RabbitMQ.Extensions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class RabbitMQTests
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
                .AddMessaging(
                    mc => mc
                        .UseJsonPacker(jc => { })
                        .UseRabbitMQ(cfb => cfb.Uri = new Uri("####")),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                );

            var serviceProvider = services.BuildServiceProvider();

            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

            var testMessage = new TestMessage();

            publisher.Publish(testMessage, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Fact]
        public void When_Received_Succsessfully_With_Json_Then_True()
        {
            var services = new ServiceCollection();

            services
                .AddLogging()
                .AddMessaging(
                    mc => mc
                        .UseJsonPacker(jss => { })
                        .UseRabbitMQ(cfb => cfb.Uri = new Uri("###")),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                );

            var serviceProvider = services.BuildServiceProvider();

            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            var router = serviceProvider.GetRequiredService<IMessageRouter>();
            var testMessage = new TestMessage();

            publisher.Publish(testMessage, CancellationToken.None).GetAwaiter().GetResult();

            router.Route(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
