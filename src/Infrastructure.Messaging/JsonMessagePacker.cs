namespace Infrastructure.Messaging
{
    using Newtonsoft.Json;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class JsonMessagePacker : IMessagePacker
    {
        public Task<object> Pack(IMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult((object)JsonConvert.SerializeObject(message, Formatting.Indented));
        }

        public Task<IMessage> Unpack(object messageObj, Type typeToUnpack, CancellationToken cancellationToken)
        {
            return Task.FromResult((IMessage)JsonConvert.DeserializeObject((string)messageObj, typeToUnpack));
        }
    }
}
