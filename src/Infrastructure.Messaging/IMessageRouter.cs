namespace Infrastructure.Messaging
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageRouter
    {
        bool IsRunning { get; }
        IEnumerable<RunningTask> RunningTasks { get; }
        Task Route(CancellationToken cancellationToken);
    }
}
