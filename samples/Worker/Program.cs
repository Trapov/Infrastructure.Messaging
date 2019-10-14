namespace Worker
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Infrastructure.Messaging.RabbitMQ.Extensions;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using Infrastructure.Messaging;
    using System.Threading.Tasks;
    using System.Text.Json;
    using Infrastructure.Messaging.InMemory.Extensions;
    using System.Runtime.CompilerServices;
    using System.Linq;
    using System.Text;

    public static class Program
    {
        public static ILogger Logger;
        public static IMessageRouter MessageRouter;
        public sealed class TestMessage : IMessage
        {
            public string Ping { get; set; } = "Ping";
        }

        public sealed class TestMessageWithEventId : IMessage
        {
            public TestMessageWithEventId(string text)
            {
                Text = text;
            }

            public Guid EventId => Guid.NewGuid();
            public string Text { get; }
        }

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
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                //_logger.LogInformation("Test message was dispached {message}", message.Ping);
                throw new Exception("AASDSD");
            }

            public Task Handle(TestMessageWithEventId message, CancellationToken cancellationToken)
            {
                //_logger.LogInformation("TestMessageWithEventId was dispatched. {eventId}, {text}", message.EventId, message.Text);
                return Task.CompletedTask;
            }
        }

        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                    .AddCommandLine(args)
                    .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile(path: $"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development"}.json", optional: true)
                    .AddEnvironmentVariables()
                .Build();

            var rabbitMQUri = configuration.GetSection("RabbitMq")["uri"];

            var serviceProvider =
                new ServiceCollection()
                    .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Trace))
                    .AddMessaging(mc =>
                        {
                            mc.UseInMemory();
                            //mc.UseRabbitMQ(cf => cf.Uri = new Uri(rabbitMQUri));
                            mc.UseJsonPacker(jso => jso.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
                        }, 
                        sc => sc
                            .AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                            .AddSingleton<IMessageHandler<TestMessageWithEventId>, TestMessageHandler>()
                    )
                .BuildServiceProvider();

            Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

            MessageRouter = serviceProvider.GetRequiredService<IMessageRouter>();
            using var cancellationTokenSource = new CancellationTokenSource();
            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            var routerTask = MessageRouter.Route(cancellationToken: cancellationTokenSource.Token);

            var renderTask = Render(cancellationToken: cancellationTokenSource.Token);

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Escape:
                        cancellationTokenSource.Cancel();
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        break;
                    case ConsoleKey.Enter:
                        var message = new TestMessage { Ping = "AAA" };
                        publisher.Publish(message, cancellationToken: cancellationTokenSource.Token);
                        break;
                    default:
                        break;
                }
            }
        }

        static async Task Render(CancellationToken cancellationToken)
        {
            var stringBuilder = new StringBuilder();
            while (!cancellationToken.IsCancellationRequested)
            {
                stringBuilder = 
                    stringBuilder
                        .Clear()
                    .AppendFormat("Router is [{0}]" + Environment.NewLine +
                            "Running tasks => {1}" + Environment.NewLine + "------------------------------------",
                            MessageRouter.IsRunning,
                            MessageRouter.RunningTasks.Count()
                        );

                lock (Console.Out)
                {
                    Console.Clear();
                    Console.Out.Write("Press [");
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Out.Write("Escape");
                    Console.ResetColor();
                    Console.Out.WriteLine("] to exit.");
                    Console.Out.Write("Press [");
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Out.Write("Enter");
                    Console.ResetColor();
                    Console.Out.WriteLine("] to queue a message." + Environment.NewLine);
                    Console.Out.WriteLine(stringBuilder.ToString());
                }
                await Task.Delay(1150, cancellationToken);
            }
        }

    }
}
