namespace Infrastructure.Messaging.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TaskExtensions
    {
        /// <summary>
        /// Adds a continuation and returns the original <seealso cref="System.Threading.Tasks.Task"/>.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task AnywayContinuation(this Task task, Action<Task> action, out Task continueTask, CancellationToken cancellationToken = default)
        {
            continueTask = task.ContinueWith(
                t => action(t),
                cancellationToken,
                TaskContinuationOptions.None | TaskContinuationOptions.LazyCancellation,
                TaskScheduler.Current
            );

            return task;
        }

    }
}
