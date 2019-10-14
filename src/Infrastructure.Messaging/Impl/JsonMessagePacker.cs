namespace Infrastructure.Messaging
{
    using System;
    using System.Threading;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.IO;
    using System.Text;

    public sealed class JsonMessagePacker : IMessagePacker
    {
        private readonly JsonSerializerOptions _options;

        public JsonMessagePacker(JsonSerializerOptions options)
        {
            _options = options;
        }

        public ValueTask<object> Pack(IMessage message, CancellationToken cancellationToken)
        {
            return new ValueTask<object>(
                JsonSerializer.Serialize(message, inputType: message.GetType(), options: _options)
            );
        }

        public async ValueTask<IMessage> Unpack(object messageObj, Type typeToUnpack, CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes((string)messageObj));

            return (IMessage)await JsonSerializer.DeserializeAsync(memoryStream, typeToUnpack, _options, cancellationToken: cancellationToken);
        }
    }
}
