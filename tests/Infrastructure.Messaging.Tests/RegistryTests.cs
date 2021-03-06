namespace Infrastructure.Messaging.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Infrastructure.Messaging.Extensions;
    using Xunit;

    public sealed class RegistryTests
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
        public void When_Registry_Returns_Valid_Handler_Then_True()
        {
            var services = new ServiceCollection();

            services
                .AddLogging()
                .AddMessaging(
                    mc => mc.UseJsonPacker(jc => { }),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                );

            var serviceProvider = services.BuildServiceProvider();

            var registry = serviceProvider.GetRequiredService<IMessageHandlersRegistry>();
            Assert.NotNull(registry);
            Assert.IsType<MessageHandlersRegistryIoC>(registry);

            var (handler, handlerType) = registry.HandlerDelegateFor(typeof(TestMessage));

            Assert.NotNull(handler);
            Assert.IsType<Handle<IMessage>>(handler);

            var message = new TestMessage();

            Assert.Equal("Ping", message.Ping);

            handler(message, serviceProvider.GetService<IMessageHandler<TestMessage>>(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal("Pong", message.Ping);
        }

        [Fact]
        public void When_Registry_Returns_Valid_Handler_From_Extension_Then_True()
        {
            var services = new ServiceCollection();

            services
                .AddLogging()
                .AddMessaging(
                    mc => mc.UseJsonPacker(jc => { }),
                    sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                );

            var serviceProvider = services.BuildServiceProvider();


            var registry = serviceProvider.GetRequiredService<IMessageHandlersRegistry>();

            Assert.NotNull(registry);
            Assert.IsType<MessageHandlersRegistryIoC>(registry);

            var (handler, handlerType) = registry.HandlerDelegateFor(typeof(TestMessage));

            Assert.NotNull(handler);
            Assert.IsType<Handle<IMessage>>(handler);

            var message = new TestMessage();

            Assert.Equal("Ping", message.Ping);

            handler(message, serviceProvider.GetService<IMessageHandler<TestMessage>>(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal("Pong", message.Ping);
        }
    }
}
