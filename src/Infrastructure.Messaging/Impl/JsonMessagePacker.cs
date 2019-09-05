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

        public async Task<object> Pack(IMessage message, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream();
            //await JsonSerializer.SerializeAsync(stream, message, message.GetType(), _options, cancellationToken);
            //await stream.FlushAsync(cancellationToken);
            //using var streamReader = new StreamReader(stream);

            //var result = await streamReader.ReadToEndAsync();
            using var writer = new Utf8JsonWriter(stream);
            var result = JsonSerializer.Serialize(message, _options);
            writer.Flush();
            return result;
        }

        public async Task<IMessage> Unpack(object messageObj, Type typeToUnpack, CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes((string)messageObj));

            return (IMessage)await JsonSerializer.DeserializeAsync(memoryStream, typeToUnpack, _options, cancellationToken: cancellationToken);
        }
    }
}
