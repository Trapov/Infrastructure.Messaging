using Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Messaging.Tests
{
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

            var connectionFactory = new ConnectionFactory { Uri = new Uri("######") };

            services
                .AddLogging()
                .AddSingleton<IConnectionFactory>(connectionFactory)
                .AddSingleton<IMessagePacker, JsonMessagePacker>()
                .AddSingleton<IMessagePublisher, RabbitMQMessagePublisher>()
                .AddSingleton<TaskFactory>()
                .AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                .AddSingleton<IMessageHandlersRegistry, MessageHandlersRegistryIoC>();

            var serviceProvider = services.BuildServiceProvider();

            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

            var testMessage = new TestMessage();

            publisher.Publish(testMessage, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
