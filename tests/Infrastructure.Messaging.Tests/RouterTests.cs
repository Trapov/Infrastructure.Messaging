﻿namespace Infrastructure.Messaging.Tests
{
    using Microsoft.Extensions.DependencyInjection;
    using Infrastructure.Messaging.Extensions.DependencyInjection;
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

            async IAsyncEnumerable<HandlingProcessFor<IMessage>> IMessageReceiver.Receive(CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield return new HandlingProcessFor<IMessage>(new TestMessage(), () => { }, (ex) => {});
            }
        }

        [Fact]
        public void When_Router_Routes_To_A_Valid_Handler_Then_Valid()
        {
            var services = new ServiceCollection();

            var messages = new[] { new TestMessage() };

            _ = services
                .AddLogging()
                .AddMessaging(
                    mc => mc.UseJsonPacker(jc => { }),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                )
                .AddSingleton<IMessageReceiver>(new MockReceiver(messages));

            var serviceProvider = services.BuildServiceProvider();

            var registry = serviceProvider.GetRequiredService<IMessageHandlersRegistry>();
            var router = serviceProvider.GetRequiredService<IMessageRouter>();

            router.Route(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
