namespace Infrastructure.Messaging
{
    using System.Collections.Generic;
    using System.Threading;

    public interface IMessageReceiver
    {
        IAsyncEnumerable<IMessage> Receive(CancellationToken cancellationToken);
    }
}
