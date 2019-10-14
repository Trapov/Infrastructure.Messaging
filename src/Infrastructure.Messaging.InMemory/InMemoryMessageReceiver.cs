namespace Infrastructure.Messaging.InMemory
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class InMemoryMessageReceiver : IMessageReceiver
    {
        private readonly IMessagePacker _messagePacker;
        private readonly BlockingCollection<(Type, object)> _memoryPipe;
        private readonly ILogger<InMemoryMessageReceiver> _logger;

        public InMemoryMessageReceiver(
            IMessagePacker messagePacker,
            ILogger<InMemoryMessageReceiver> logger,
            BlockingCollection<(Type, object)> memoryPipe)
        {
            _messagePacker = messagePacker;
            _memoryPipe = memoryPipe;
            _logger = logger;

        }

        public async IAsyncEnumerable<HandlingProcessFor<IMessage>> Receive([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            while(!cancellationToken.IsCancellationRequested)
            {
                HandlingProcessFor<IMessage> process = default;
                try
                {
                    (Type Type, object Message) message;
                    while (!_memoryPipe.TryTake(out message, 25, cancellationToken))
                        await Task.Delay(100).ConfigureAwait(false);

                    var unpackedMessage = await _messagePacker.Unpack(messageObj: message.Message, typeToUnpack: message.Type, cancellationToken: cancellationToken);

                    process = new HandlingProcessFor<IMessage>(
                        message: unpackedMessage,
                        toHandled: () => { },
                        toError: (e) => { }
                    );

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "While trying to receive a message there was an error");
                    continue;
                }

                yield return process;
            }
        }
    }
}
