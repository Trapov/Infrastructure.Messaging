namespace Infrastructure.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class DefaultMessageRouter : IMessageRouter
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageHandlersRegistry _messageHandlersRegistry;

        public DefaultMessageRouter(
            IMessageReceiver messageReceiver, 
            IMessageHandlersRegistry messageHandlersRegistry)
        {
            _messageReceiver = messageReceiver;
            _messageHandlersRegistry = messageHandlersRegistry;
        }

        public async Task Route(CancellationToken cancellationToken)
        {
            await foreach(var message in _messageReceiver.Receive(cancellationToken))
            {
                var messageType = message.GetType();
                var handlerDelegate = _messageHandlersRegistry.HandlerDelegateFor(messageType);

                handlerDelegate(message, cancellationToken);
            }
        }
    }
}
