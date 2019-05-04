namespace Infrastructure.Messaging.Extensions.DependencyInjection
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
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

            serviceCollection
                .AddSingleton(serviceCollection)
                .AddSingleton(new TaskFactory())
                .AddSingleton<IMessageRouter, DefaultMessageRouter>();
            return registerHandlersBuilder(serviceCollection);
        }

        public static IServiceCollection AddIoCRegistryWithHandlers(
            this IServiceCollection serviceCollection,
            params (Type intrf, Type impl)[] messageHandlerTypes)
        {
            serviceCollection.AddSingleton(serviceCollection);
            serviceCollection.AddSingleton<IMessageHandlersRegistry, MessageHandlersRegistryIoC>();

            foreach (var (intrf, impl) in messageHandlerTypes)
                serviceCollection.AddSingleton(intrf, impl);

            return serviceCollection;
        }
    }
}
