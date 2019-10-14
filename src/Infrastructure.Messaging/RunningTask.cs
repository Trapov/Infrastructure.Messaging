namespace Infrastructure.Messaging
{
    using System.Threading.Tasks;

    public sealed class RunningTask
    {
        public RunningTask(string name, Task task)
        {
            Name = name;
            Task = task;
        }

        public string Name { get; }
        public Task Task { get; }
    }
}
