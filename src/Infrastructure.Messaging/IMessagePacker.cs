namespace Infrastructure.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessagePacker
    {
        Task<object> Pack(
            IMessage message,
            CancellationToken cancellationToken);
        Task<IMessage> Unpack(
            object messageObj,
            Type typeToUnpack,
            CancellationToken cancellationToken);
    }
}
