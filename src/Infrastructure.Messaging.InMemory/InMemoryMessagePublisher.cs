namespace Infrastructure.Messaging.InMemory
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class InMemoryMessagePublisher : IMessagePublisher
    {
        private readonly IMessagePacker _messagePacker;
        private readonly BlockingCollection<(Type, object)> _memoryPipe; 

        public InMemoryMessagePublisher(
            IMessagePacker messagePacker,
            BlockingCollection<(Type, object)> blockingCollection)
        {
            _messagePacker = messagePacker;
            _memoryPipe = blockingCollection;
        }

        public async Task Publish(IMessage message, CancellationToken cancellationToken)
        {
            var packedMessage = await _messagePacker.Pack(message: message, cancellationToken);
            _memoryPipe.Add((message.GetType(), packedMessage));
        }
    }
}
