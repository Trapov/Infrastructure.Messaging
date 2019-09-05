namespace Infrastructure.Messaging.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ITaskExtensions
    {
        public static Task OnError(this Task task, Action<Exception> action, CancellationToken cancellationToken = default)
        {
            task.ContinueWith(
                t => action(t.Exception),
                cancellationToken,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Current
            );

            return task;
        }

        public static Task Anyway(this Task task, Action<int> action, CancellationToken cancellationToken = default)
        {
            task.ContinueWith(
                t => action(t.Id),
                cancellationToken,
                TaskContinuationOptions.None | TaskContinuationOptions.LazyCancellation,
                TaskScheduler.Current
            );

            return task;
        }

        public static Task OnSuccess(this Task task, Action action, CancellationToken cancellationToken = default)
        {
            task.ContinueWith(
                _ => action(),
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LazyCancellation,
                TaskScheduler.Current
            );

            return task;
        }
    }
}
