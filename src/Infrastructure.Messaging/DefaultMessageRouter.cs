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
            await foreach(var handlingProcess in _messageReceiver.Receive(cancellationToken))
            {
                var messageType = handlingProcess.Message.GetType();
                var handlerDelegate = _messageHandlersRegistry.HandlerDelegateFor(messageType);

                handlerDelegate(handlingProcess.Message, cancellationToken)
                    .ContinueWith(t => handlingProcess.ToHandled(), cancellationToken);
            }
        }
    }
}
