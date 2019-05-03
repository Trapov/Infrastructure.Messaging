namespace Infrastructure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using Microsoft.Extensions.Logging;

    public sealed class MessageHandlersRegistryIoC : IMessageHandlersRegistry
    {
        private readonly ILogger<MessageHandlersRegistryIoC> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly IDictionary<Type, Type> _messageTypeToHandlerType;
        private readonly IDictionary<Type, Delegate> _messageTypeToDelegateType;

        public MessageHandlersRegistryIoC(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<MessageHandlersRegistryIoC>();
            _serviceProvider = serviceProvider;
            _messageTypeToHandlerType = new Dictionary<Type, Type>();
            _messageTypeToDelegateType = new Dictionary<Type, Delegate>();
        }

        public void Register<TMessageHandler>()
        {
            var messageHandlerType = typeof(TMessageHandler);
            var messageType = messageHandlerType.GetGenericArguments().First();

            _messageTypeToHandlerType.Add(messageType, messageHandlerType);
        }

        public IMessageHandler For(Type messageType)
        {
            var messageHandlerType = _messageTypeToHandlerType[messageType];

            _logger.LogTrace("Trying to get a message handler of the type {0}.", messageHandlerType);
            var messageHandler = _serviceProvider.GetService(messageHandlerType);

            return (IMessageHandler) messageHandler;
        }

        public Delegate AsDelegate(IMessageHandler handler)
        {
            var handlerTypeBase = handler.GetType().GetInterfaces().First(i => typeof(IMessageHandler).IsAssignableFrom(i) && i.IsGenericType);
            var messageType = handlerTypeBase.GetGenericArguments().First();

            if(_messageTypeToDelegateType.TryGetValue(messageType, out var @delegate))
                return @delegate;

            var handlerParam = Expression.Constant(handler);

            var messageParam = Expression.Parameter(messageType, "message");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var expression = Expression.Lambda(
                Expression.Block(
                    Expression.Call(
                        handlerParam,
                        handlerParam.Type.GetMethod("Handle"),
                        messageParam,
                        cancellationTokenParam
                    )
                ),
                messageParam,
                cancellationTokenParam);

            var act = expression.Compile();
            _messageTypeToDelegateType.Add(messageType,act);
            return act;
        }
    }
}
