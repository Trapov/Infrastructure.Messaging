namespace Infrastructure.Messaging.Tests
{
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class RouterTest
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

        public sealed class MockReceiver : IMessageReceiver
        {
            private readonly IReadOnlyList<IMessage> _messages;

            public MockReceiver(IReadOnlyList<IMessage> messages)
            {
                _messages = messages;
            }

            public IAsyncEnumerable<IMessage> Receive(CancellationToken cancellationToken)
            {
                return AsyncEnumerable.Range(0, 1).Select(d => new TestMessage());
            }
        }

        [Fact]
        public void When_Router_Routes_To_A_Valid_Handler_Then_Valid()
        {
            var services = new ServiceCollection();

            var messages = new[] { new TestMessage() };

            services
                .AddLogging()
                .AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                .AddSingleton<IMessageRouter, DefaultMessageRouter>()
                .AddSingleton<IMessageReceiver>(new MockReceiver(messages))
                .AddSingleton<IMessageHandlersRegistry, MessageHandlersRegistryIoC>();

            var serviceProvider = services.BuildServiceProvider();

            var registry = serviceProvider.GetRequiredService<IMessageHandlersRegistry>();
            registry.Register<IMessageHandler<TestMessage>>();
            var router = serviceProvider.GetRequiredService<IMessageRouter>();

            router.Route(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
