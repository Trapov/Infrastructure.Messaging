namespace Infrastructure.Messaging
{
    using System.Threading.Tasks;
    using System.Threading;

    public interface IMessageHandler<in TMessage> : IMessageHandler
        where TMessage : IMessage
    {
        Task Handle(TMessage message, CancellationToken cancellationToken);
    }

    public interface IMessageHandler { }
}