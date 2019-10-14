namespace Infrastructure.Messaging.InMemory.Extensions
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Concurrent;

    public static class MessagingConfigurationExtensions
    {
        public static MessagingConfiguration UseInMemory(this MessagingConfiguration messagingConfiguration)
        {

            messagingConfiguration
                .Services
                    .AddSingleton<BlockingCollection<(Type, object)>, BlockingCollection<(Type, object)>>()
                    .AddSingleton<IMessageReceiver, InMemoryMessageReceiver>()
                    .AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();

            return messagingConfiguration;
        }
    }
}
