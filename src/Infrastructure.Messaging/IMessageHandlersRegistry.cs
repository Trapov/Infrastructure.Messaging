namespace Infrastructure.Messaging
{
    using System;

    public interface IMessageHandlersRegistry
    {
        void Register<TMessageHandler>();
        IMessageHandler For(Type type);

        Delegate AsDelegate(IMessageHandler handler);
    }
}
