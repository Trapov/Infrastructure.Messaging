namespace Infrastructure.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessagePacker
    {
        ValueTask<object> Pack(
            IMessage message,
            CancellationToken cancellationToken);
        ValueTask<IMessage> Unpack(
            object messageObj,
            Type typeToUnpack,
            CancellationToken cancellationToken);
    }
}
