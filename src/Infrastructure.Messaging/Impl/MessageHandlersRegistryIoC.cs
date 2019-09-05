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

        public IDictionary<Type, (Handle<IMessage>, Type)> MessageTypeToDelegateType { get; }

        public MessageHandlersRegistryIoC(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            IServiceCollection serviceCollection)
        {
            _logger = loggerFactory.CreateLogger<MessageHandlersRegistryIoC>();
            _serviceProvider = serviceProvider;
            MessageTypeToDelegateType = new Dictionary<Type, (Handle<IMessage>, Type)>();

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

            //var handler = _serviceProvider.GetService(messageHandlerType);

            var handlerParam = Expression.Parameter(typeof(IMessageHandler), "handler");
            var messageParam = Expression.Parameter(typeof(IMessage), "message");

            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var messageVariableExact = Expression.Variable(messageType, "messageExact");
            var handlerVariableExact = Expression.Variable(messageHandlerType, "handlerExact");

            var block = Expression.Block(
                    new[] { messageVariableExact, handlerVariableExact },
                    Expression.Assign(handlerVariableExact, Expression.Convert(handlerParam, messageHandlerType)),
                    Expression.Assign(messageVariableExact, Expression.Convert(messageParam, messageType)),
                    Expression.Call(
                        handlerVariableExact,
                        handlerVariableExact.Type.GetMethod("Handle", new[] { messageType, typeof(CancellationToken) }),
                        messageVariableExact,
                        cancellationTokenParam
                    )
                );

            var expression = Expression.Lambda<Func<IMessage, IMessageHandler, CancellationToken, Task>>(
                block,
                messageParam,
                handlerParam,
                cancellationTokenParam);

            var handleFunc = (Handle<IMessage>)expression.Compile().Invoke;
            MessageTypeToDelegateType.Add(messageType, (handleFunc, messageHandlerType));
        }
    }
}
