namespace Infrastructure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public sealed class MessageHandlersRegistryIoC : IMessageHandlersRegistry
    {
        private readonly ILogger<MessageHandlersRegistryIoC> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<Type, Handle<IMessage>> _messageTypeToDelegateType;

        public MessageHandlersRegistryIoC(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<MessageHandlersRegistryIoC>();
            _serviceProvider = serviceProvider;
            _messageTypeToDelegateType = new Dictionary<Type, Handle<IMessage>>();
        }

        public void Register<TMessageHandler>()
            where TMessageHandler : IMessageHandler
        {
            var messageHandlerType = typeof(TMessageHandler);
            var messageType = messageHandlerType.GetGenericArguments().First();

            var handler = _serviceProvider.GetService(typeof(TMessageHandler));

            var handlerParam = Expression.Constant(handler);

            var messageParam = Expression.Parameter(typeof(IMessage), "message");
            var messageVariableExact = Expression.Variable(messageType, "messageExact");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var expression = Expression.Lambda<Func<IMessage, CancellationToken, Task>>(
                Expression.Block(
                    new[] { messageVariableExact },
                    Expression.Assign(messageVariableExact, Expression.Convert(messageParam, messageType)),
                    Expression.Call(
                        handlerParam,
                        handlerParam.Type.GetMethod("Handle"),
                        messageVariableExact,
                        cancellationTokenParam
                    )
                ),
                messageParam,
                cancellationTokenParam);

            var handleFunc = (Handle<IMessage>)expression.Compile().Invoke;

            _messageTypeToDelegateType.Add(messageType, handleFunc);
        }


        public Handle<IMessage> HandlerDelegateFor(Type messageType)
        {
            return _messageTypeToDelegateType[messageType];
        }
    }
}
