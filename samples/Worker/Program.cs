namespace Worker
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Infrastructure.Messaging.Extensions.DependencyInjection;
    using Infrastructure.Messaging.RabbitMQ.Extensions;
    using System;
    using System.IO;
    using Newtonsoft.Json.Serialization;
    using System.Runtime.Loader;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using Infrastructure.Messaging;
    using System.Threading.Tasks;

    public static class Program
    {
        public static ILogger Logger;

        public sealed class TestMessage : IMessage
        {
            public string Ping { get; set; } = "Ping";
        }
        public sealed class TestMessageHandler : IMessageHandler<TestMessage>
        {
            private readonly ILogger<TestMessageHandler> _logger;

            public TestMessageHandler(ILogger<TestMessageHandler> logger)
            {
                _logger = logger;
            }

            public Task Handle(TestMessage message, CancellationToken cancellationToken)
            {
                message.Ping = "Pong";
                _logger.LogInformation("TestMessage was dispatched. {message}", message);
                return Task.CompletedTask;
            }
        }

        public static void Main(string[] args)
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
                    .AddLogging(lb => lb.AddConsole())
                    .AddMessaging(mc =>
                        {
                            mc.UseRabbitMQ(cf => cf.Uri = new Uri(rabbitMQUri));
                            mc.UseJsonPacker(jss => jss.ContractResolver = new CamelCasePropertyNamesContractResolver());
                        }, 
                        sc => sc.AddSingleton<IMessageHandler<TestMessage>, TestMessageHandler>()
                    )
                .BuildServiceProvider();

            Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

            var router = serviceProvider.GetRequiredService<IMessageRouter>();

            var cancellationTokenSource = new CancellationTokenSource();

            router.Route(cancellationTokenSource.Token);
            WaitUntillEnd(cancellationTokenSource);
        }

        private static void WaitUntillEnd(CancellationTokenSource cancellationTokenSource)
        {
            var ended = new ManualResetEventSlim();
            var starting = new ManualResetEventSlim();

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Logger.LogInformation("Unloading fired");
                starting.Set();
                Logger.LogInformation("Waiting for completion");
                cancellationTokenSource.Cancel();
                ended.Wait();
            };

            Logger.LogInformation("Waiting for signals");
            starting.Wait();

            Logger.LogInformation("Received signal gracefully shutting down");
            Thread.Sleep(5000);
            ended.Set();
        }
    }
}
