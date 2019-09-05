namespace Infrastructure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate Task Handle<in TMessage>(
        TMessage message,
        IMessageHandler messageHandler,
        CancellationToken cancellationToken)
        where TMessage : IMessage;

    public interface IMessageHandlersRegistry
    {
        IDictionary<Type, (Handle<IMessage>, Type)> MessageTypeToDelegateType { get; }
        void Register(Type messageHandlerType);
    }
}
