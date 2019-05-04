namespace Infrastructure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public sealed class MessageHandlersRegistryIoC : IMessageHandlersRegistry
    {
        private readonly ILogger<MessageHandlersRegistryIoC> _logger;
        private readonly IServiceProvider _serviceProvider;

        public IDictionary<Type, Handle<IMessage>> MessageTypeToDelegateType { get; }

        public MessageHandlersRegistryIoC(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            IServiceCollection serviceCollection)
        {
            _logger = loggerFactory.CreateLogger<MessageHandlersRegistryIoC>();
            _serviceProvider = serviceProvider;
            MessageTypeToDelegateType = new Dictionary<Type, Handle<IMessage>>();

            var handlers = serviceCollection
                .Where(sd =>
                {
                    return 
                        sd.ServiceType != null &&
                        typeof(IMessageHandler).IsAssignableFrom(sd.ServiceType);
                });

            foreach (var handler in handlers)
                Register(handler.ServiceType);
        }

        public void Register(Type messageHandlerType)
        {
            var messageType = messageHandlerType.GetGenericArguments().First();

            if (MessageTypeToDelegateType.ContainsKey(messageType))
                return;

            var handler = _serviceProvider.GetService(messageHandlerType);

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

            MessageTypeToDelegateType.Add(messageType, handleFunc);
        }

        public Handle<IMessage> HandlerDelegateFor(Type messageType) => MessageTypeToDelegateType[messageType];
    }
}
