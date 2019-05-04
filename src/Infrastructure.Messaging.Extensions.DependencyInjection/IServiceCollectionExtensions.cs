namespace Infrastructure.Messaging.Extensions.DependencyInjection
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMessaging(
            this IServiceCollection serviceCollection,
            Action<MessagingConfiguration> messagingBuilder,
            Func<IServiceCollection, IServiceCollection> registerHandlersBuilder)
        {
            var messagingConfiguration = new MessagingConfiguration(serviceCollection);
            messagingBuilder(messagingConfiguration);

            if (!serviceCollection.Any(sd => typeof(TaskFactory).IsAssignableFrom(sd.ServiceType)))
                serviceCollection.AddSingleton(new TaskFactory());
            if (!serviceCollection.Any(sd => typeof(IServiceCollection).IsAssignableFrom(sd.ServiceType)))
                serviceCollection.AddSingleton<IServiceCollection>(serviceCollection);
            if (!serviceCollection.Any(sd => typeof(IMessageHandlersRegistry).IsAssignableFrom(sd.ServiceType)))
                serviceCollection.AddSingleton<IMessageHandlersRegistry, MessageHandlersRegistryIoC>();
            if (!serviceCollection.Any(sd => typeof(IMessageRouter).IsAssignableFrom(sd.ServiceType)))
                serviceCollection.AddSingleton<IMessageRouter, DefaultMessageRouter>();

            return registerHandlersBuilder(serviceCollection);
        }
    }
}
