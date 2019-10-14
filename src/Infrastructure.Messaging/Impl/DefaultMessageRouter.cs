namespace Infrastructure.Messaging
{
    using Infrastructure.Messaging.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class DefaultMessageRouter : IMessageRouter
    {
        private const string LogMessageHandlingTemplate = "Thread [{ManagedThreadId}] is trying to dispatch a message [{MessageType}] to [{Handler}]";
        private const string LogMessageRemovedTemplate = "[{MessageType}]:[{TaskId}] was handled";
        private const string LogMessageAddedForHandling = "Thread [{ManagedThreadId}] has placed [{MessageType}]:[{TaskId}] for dispatching";


        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageHandlersRegistry _messageHandlersRegistry;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<int, RunningTask> _runningTasks;
        private readonly ILogger<DefaultMessageRouter> _logger;
        private readonly SpinWait _spinForAdd = new SpinWait();
        private readonly SpinWait _spinForRemove = new SpinWait();


        public bool IsRunning { get; private set; }
        
        // Will do a snapshot so we're fine. 
        public IEnumerable<RunningTask> RunningTasks => _runningTasks.Select(t => t.Value);

        public DefaultMessageRouter(
            IMessageReceiver messageReceiver,
            IMessageHandlersRegistry messageHandlersRegistry,
            ILogger<DefaultMessageRouter> logger,
            IServiceProvider serviceProvider)
        {
            _messageReceiver = messageReceiver;
            _messageHandlersRegistry = messageHandlersRegistry;
            _serviceProvider = serviceProvider;

            _logger = logger;
            _runningTasks = new ConcurrentDictionary<int, RunningTask>();
        }

        public async Task Route(CancellationToken cancellationToken)
        {
            if (IsRunning)
                throw new InvalidOperationException($"The [{nameof(DefaultMessageRouter)}] is already running.");
            else
                IsRunning = true;

            await foreach (var handlingProcess in _messageReceiver.Receive(cancellationToken))
            {
                var messageType = handlingProcess.Message.GetType();
                var (handlerDelegate, handlerType) = _messageHandlersRegistry.HandlerDelegateFor(messageType);

                using var scope = _serviceProvider.CreateScope();
                var handler = (IMessageHandler)scope.ServiceProvider.GetService(handlerType);
                
                _logger.LogTrace(
                    message: LogMessageHandlingTemplate, 
                    Thread.CurrentThread.ManagedThreadId, messageType, handlerType
                );

                // We do busy-waiting instead of Task.Delay because it's cheaper to await on remove/add-misses because they're quick, rather than yield to another thread.
                var handlerTask = ToHandler(
                    handler: handler,
                    handlingProcess: handlingProcess,
                    handlerDelegate: handlerDelegate,
                    cancellationToken: cancellationToken
                ).AnywayContinuation(action: task =>
                {
                    // Busy-waiting
                    RunningTask removedTask;
                    while (!_runningTasks.TryRemove(task.Id, out removedTask))
                        _spinForRemove.SpinOnce();

                    _logger.LogTrace(
                         message: LogMessageRemovedTemplate, removedTask.Name, removedTask.Task.Id
                     );

                }, continueTask: out _, cancellationToken: cancellationToken);

                // Busy-waiting
                while (!_runningTasks.TryAdd(handlerTask.Id, new RunningTask(messageType.Name, handlerTask)))
                    _spinForAdd.SpinOnce();

                _logger.LogTrace(
                     message: LogMessageAddedForHandling, Thread.CurrentThread.ManagedThreadId, messageType.Name, handlerTask.Id
                 );
            }
        }

        private static async Task ToHandler(
            IMessageHandler handler,
            HandlingProcessFor<IMessage> handlingProcess,
            Handle<IMessage> handlerDelegate,
            CancellationToken cancellationToken)
        {
            // If handler runs synchronosly then it might block the others so we will yield to another thread implicitly.
            await Task.Yield();

            try
            {
                await handler
                    .Handle(handlingProcess.Message, handlerDelegate, cancellationToken)
                    .ConfigureAwait(false);

                handlingProcess.ToHandled();
            }
            catch (Exception exception)
            {
                handlingProcess.ToError(exception);
            }
        }
    }
}
