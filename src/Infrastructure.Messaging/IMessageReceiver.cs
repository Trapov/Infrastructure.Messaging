namespace Infrastructure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public interface IMessageReceiver
    {
        IAsyncEnumerable<HandlingProcessFor<IMessage>> Receive(CancellationToken cancellationToken);
    }

    public sealed class HandlingProcessFor<TMessage>
    {
        public HandlingProcessFor(IMessage message, Action toHandled)
        {
            Message = message;
            ToHandled = toHandled;
        }

        public IMessage Message { get; }
        public Action ToHandled { get; }
    }
}
