namespace Infrastructure.Messaging.Tests
{
    using Infrastructure.Messaging.RabbitMQ;
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using global::RabbitMQ.Client;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using System.Linq;

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

            var connectionFactory = new ConnectionFactory { Uri = new Uri("####") };

            services
                .AddLogging()
                .AddSingleton<IConnectionFactory>(connectionFactory)
                .AddSingleton<IMessagePacker, JsonMessagePacker>()
                .AddSingleton<IMessagePublisher, RabbitMQMessagePublisher>()
                .AddSingleton<TaskFactory>()
                .AddIoCRegistryWithHandlers(
                    (typeof(IMessageHandler<TestMessage>), typeof(TestMessageHandler))
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

            var connectionFactory = new ConnectionFactory { Uri = new Uri("###") };

            services
                .AddLogging()
                .AddSingleton<IConnectionFactory>(connectionFactory)
                .AddSingleton<IMessagePacker, JsonMessagePacker>()
                .AddSingleton<IMessagePublisher, RabbitMQMessagePublisher>()
                .AddSingleton<IMessageRouter, DefaultMessageRouter>()
                .AddSingleton<IMessageReceiver, RabbitMQMessageReceiver>()
                .AddSingleton<TaskFactory>()
                .AddIoCRegistryWithHandlers(
                    (typeof(IMessageHandler<TestMessage>), typeof(TestMessageHandler))
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
