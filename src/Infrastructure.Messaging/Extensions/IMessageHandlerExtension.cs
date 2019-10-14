namespace Infrastructure.Messaging.Extensions
{
    using System.Threading;
    using System.Threading.Tasks;

    public static class IMessageHandlerExtension
    {
        public static Task Handle(this IMessageHandler messageHandler, IMessage message, Handle<IMessage> handle, CancellationToken cancellationToken = default)
        {
            return handle(message, messageHandler, cancellationToken);
        }
    }
}
