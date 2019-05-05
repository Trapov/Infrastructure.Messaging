namespace Infrastructure.Messaging.InMemory
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public sealed class InMemoryMessageReceiver : IMessageReceiver
    {
        private readonly IMessagePacker _messagePacker;
        private readonly BlockingCollection<(Type, object)> _memoryPipe;

        public InMemoryMessageReceiver(
            IMessagePacker messagePacker,
            BlockingCollection<(Type, object)> memoryPipe)
        {
            _messagePacker = messagePacker;
            _memoryPipe = memoryPipe;
        }

        public IAsyncEnumerable<HandlingProcessFor<IMessage>> Receive(CancellationToken cancellationToken)
        {
            return _memoryPipe.GetConsumingEnumerable(cancellationToken)
                .Select(pm => new HandlingProcessFor<IMessage>(_messagePacker.Unpack(pm.Item2, pm.Item1, cancellationToken).GetAwaiter().GetResult(), () => { }, (_) => { }))
                .ToAsyncEnumerable();
        }
    }
}
