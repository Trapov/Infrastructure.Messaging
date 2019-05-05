namespace Infrastructure.Messaging
{
    using Infrastructure.Messaging.Extensions;
    using System;
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

                try
                {
                    var handlerTask = handlerDelegate(handlingProcess.Message, cancellationToken);

                    handlerTask
                        .ContinueWith(t =>handlingProcess.ToError(t.Exception),
                        cancellationToken,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Current);
                    
                    handlerTask
                        .ContinueWith(t => handlingProcess.ToHandled(),
                        cancellationToken,
                        TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.Current);
                }
                catch(Exception ex)
                {
                    handlingProcess.ToError(ex);
                }
            }
        }
    }
}
