namespace Infrastructure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate Task Handle<in TMessage>(
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : IMessage;

    public interface IMessageHandlersRegistry
    {
        IDictionary<Type, Handle<IMessage>> MessageTypeToDelegateType { get; }
        void Register(Type messageHandlerType);
    }
}
