namespace Infrastructure.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessagePublisher
    {
        Task Publish(IMessage message, CancellationToken cancellationToken);
    }
}
