namespace Infrastructure.Messaging
{
    using Infrastructure.Messaging.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class DefaultMessageRouter : IMessageRouter
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageHandlersRegistry _messageHandlersRegistry;
        private readonly IServiceProvider _serviceProvider;

        public DefaultMessageRouter(
            IMessageReceiver messageReceiver,
            IMessageHandlersRegistry messageHandlersRegistry,
            IServiceProvider serviceProvider)
        {
            _messageReceiver = messageReceiver;
            _messageHandlersRegistry = messageHandlersRegistry;
            _serviceProvider = serviceProvider;
        }

        public async Task Route(CancellationToken cancellationToken)
        {
            await foreach(var handlingProcess in _messageReceiver.Receive(cancellationToken))
            {
                var messageType = handlingProcess.Message.GetType();
                var (handlerDelegate, handlerType) = _messageHandlersRegistry.HandlerDelegateFor(messageType);

                using var scope = _serviceProvider.CreateScope();
                var handler = (IMessageHandler) scope.ServiceProvider.GetService(handlerType);

                try
                {
                    var handlerTask = handlerDelegate(handlingProcess.Message, handler, cancellationToken);

                    handlerTask
                        .ContinueWith(
                            t => handlingProcess.ToError(t.Exception),
                            cancellationToken,
                            TaskContinuationOptions.OnlyOnFaulted,
                            TaskScheduler.Current
                        );
                    
                    handlerTask
                        .ContinueWith(
                            t => handlingProcess.ToHandled(),
                            cancellationToken,
                            TaskContinuationOptions.OnlyOnRanToCompletion,
                            TaskScheduler.Current
                        );
                }
                catch(Exception ex)
                {
                    handlingProcess.ToError(ex);
                }
            }
        }
    }
}
