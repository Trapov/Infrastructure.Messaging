namespace Infrastructure.Messaging.Extensions.DependencyInjection
{
    using Microsoft.Extensions.DependencyInjection;
    using System;

    public static class IServiceCollectionExtensions
    {
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
