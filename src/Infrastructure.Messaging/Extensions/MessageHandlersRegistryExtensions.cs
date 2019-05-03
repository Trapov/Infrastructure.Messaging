namespace Infrastructure.Messaging.Extensions
{
    using System.Linq;

    public static class MessageHandlersRegistryExtensions
    {
        public static TMessageHandler Get<TMessageHandler>(this IMessageHandlersRegistry messageHandlersRegistry)
        {
            var handlerType = typeof(TMessageHandler);
            var messageType = handlerType.GetGenericArguments().First();

            return (TMessageHandler) messageHandlersRegistry.For(messageType);
        }
    }
}
