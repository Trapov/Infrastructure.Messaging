namespace Infrastructure.Messaging
{
    using Infrastructure.Messaging.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class DefaultMessageRouter : IMessageRouter
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageHandlersRegistry _messageHandlersRegistry;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<int, (string Name, Task Task)> _runningTasks;
        private readonly ILoggerFactory _loggerFactory;

        public DefaultMessageRouter(
            IMessageReceiver messageReceiver,
            IMessageHandlersRegistry messageHandlersRegistry,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            _messageReceiver = messageReceiver;
            _messageHandlersRegistry = messageHandlersRegistry;
            _serviceProvider = serviceProvider;

            _loggerFactory = loggerFactory;
            _runningTasks = new ConcurrentDictionary<int, (string Name, Task Task)>();
        }

        public async Task Route(CancellationToken cancellationToken)
        {
            await foreach(var handlingProcess in _messageReceiver.Receive(cancellationToken))
            {
                var logger = _loggerFactory.CreateLogger<DefaultMessageRouter>();
                logger.LogTrace("Running handlers => [{0}] || [{1}].", _runningTasks.Count, _runningTasks.Values.Select(pair => $"Id:{pair.Task.Id} Name:{pair.Name}"));

                var messageType = handlingProcess.Message.GetType();
                var (handlerDelegate, handlerType) = _messageHandlersRegistry.HandlerDelegateFor(messageType);

                using var scope = _serviceProvider.CreateScope();
                var handler = (IMessageHandler) scope.ServiceProvider.GetService(handlerType);

                try
                {
                    var handlerTask = handler
                            .Handle(handlingProcess.Message, handlerDelegate, cancellationToken)
                                .OnError(e => handlingProcess.ToError(e), cancellationToken)
                                .OnSuccess(() => handlingProcess.ToHandled(), cancellationToken)
                            .Anyway(id => _runningTasks.TryRemove(id, out _));

                    _runningTasks.TryAdd(handlerTask.Id, (Name: handlingProcess.Message.GetType().Name, Task: handlerTask));
                }
                catch(Exception ex)
                {
                    handlingProcess.ToError(ex);
                }
            }
        }
    }
}
