namespace Infrastructure.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageRouter
    {
        Task Route(CancellationToken cancellationToken);
    }
}
