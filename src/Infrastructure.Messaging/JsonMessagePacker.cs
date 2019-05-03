namespace Infrastructure.Messaging
{
    using Newtonsoft.Json;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class JsonMessagePacker : IMessagePacker
    {
        private readonly TaskFactory _taskFactory;

        public JsonMessagePacker(
            TaskFactory taskFactory)
        {
            _taskFactory = taskFactory;
        }

        public Task<object> Pack(IMessage message, CancellationToken cancellationToken)
        {
            return _taskFactory.StartNew(() =>
            {
                return (object)JsonConvert.SerializeObject(message, Formatting.Indented);
            }, cancellationToken);
        }

        public Task<IMessage> Unpack(object messageObj, Type typeToUnpack, CancellationToken cancellationToken)
        {
            return _taskFactory.StartNew(() =>
            {
                return (IMessage)JsonConvert.DeserializeObject((string)messageObj, typeToUnpack);
            }, cancellationToken);
        }
    }
}
