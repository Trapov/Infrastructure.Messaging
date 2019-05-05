namespace Infrastructure.Messaging.Extensions
{
    using System;

    public static class IMessageHandlersRegistryExtensions
    {
        public static void Register<TMessageHandler>(
            this IMessageHandlersRegistry messageHandlersRegistry) where TMessageHandler : IMessageHandler
        {
            messageHandlersRegistry.Register(typeof(TMessageHandler));
        }

        public static Handle<IMessage> HandlerDelegateFor(
            this IMessageHandlersRegistry messageHandlersRegistry, 
            Type messageType) => messageHandlersRegistry.MessageTypeToDelegateType[messageType];
    }
}
