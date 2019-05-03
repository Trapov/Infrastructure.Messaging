namespace Infrastructure.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate Task Handle<in TMessage>(
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : IMessage;

    public interface IMessageHandlersRegistry
    {
        void Register<TMessageHandler>() where TMessageHandler : IMessageHandler;
        Handle<IMessage> HandlerDelegateFor(Type messageType);
    }
}
