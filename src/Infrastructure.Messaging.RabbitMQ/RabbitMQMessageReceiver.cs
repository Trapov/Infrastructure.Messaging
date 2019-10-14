namespace Infrastructure.Messaging.RabbitMQ
{
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;
    using global::RabbitMQ.Client.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class RabbitMQMessageReceiver : IMessageReceiver
    {
        private readonly Lazy<IConnection> _connection;
        private readonly TaskFactory _taskFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessagePacker _messagePacker;
        private readonly BlockingCollection<HandlingProcessFor<IMessage>> _memoryPipe;

        public RabbitMQMessageReceiver(
            IConnectionFactory connectionFactory,
            TaskFactory taskFactory,
            IMessageHandlersRegistry messageHandlersRegistry,
            ILoggerFactory loggerFactory,
            IMessagePacker messagePacker)
        {
            _memoryPipe = new BlockingCollection<HandlingProcessFor<IMessage>>();
            _connection = new Lazy<IConnection>(() =>
            {
                var logger = _loggerFactory.CreateLogger<RabbitMQMessageReceiver>();

                var tryCount = 0;
                var timeOut = TimeSpan.FromSeconds(4);

                while (tryCount != 20)
                {
                    Interlocked.Increment(ref tryCount);
                    try
                    {
                        var connection = connectionFactory.CreateConnection(Assembly.GetEntryAssembly().FullName);
                        logger.LogTrace("Connection is succesfull => {connection}", connection);
                        return connection;
                    }
                    catch (BrokerUnreachableException e)
                    {
                        logger.LogError("Can't connect to the RabbitMq bus. Trying to reconnect in {timeOut} | Try[{tryCount}]. Exception {e}", timeOut, tryCount, e);
                        Task.Delay(timeOut).GetAwaiter().GetResult();
                    }
                }
                throw new Exception("No connection to the event bus");
            });

            _taskFactory = taskFactory;
            _loggerFactory = loggerFactory;
            _messagePacker = messagePacker;

            RegisterAll(messageHandlersRegistry.MessageTypeToDelegateType);
        }

        public async IAsyncEnumerable<HandlingProcessFor<IMessage>> Receive(CancellationToken cancellationToken)
        {
            await Task.Yield();
            foreach (var message in _memoryPipe.GetConsumingEnumerable(cancellationToken))
            {
                yield return message;
            }
        }

        private void RegisterAll(IDictionary<Type, (Handle<IMessage>, Type)> messageTypeToDelegateType)
        {
            foreach (var messageType in messageTypeToDelegateType.Keys)
            {
                var eventType = messageType;
                var exchangeName = eventType.Namespace.Split('.').First();
                var routingKey = eventType.Name;
                var queueName = $"{exchangeName}.{routingKey}";

                var consumer = _connection.Value.CreateModel();

                consumer.ExchangeDeclare(
                    exchange: exchangeName,
                    durable: false,
                    type: ExchangeType.Direct);

                consumer.QueueDeclare(
                    queue: queueName,
                    exclusive: false,
                    autoDelete: false,
                    durable: true);

                consumer.QueueBind(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: routingKey);

                var _eventConsumer = new EventingBasicConsumer(consumer);
                _eventConsumer.Received += (_model, ea) =>
                {
                    var model = ((EventingBasicConsumer)_model).Model;

                    var message = _messagePacker
                    .Unpack(
                        Encoding.UTF8.GetString(ea.Body),
                        eventType,
                        CancellationToken.None)
                    .GetAwaiter().GetResult();

                    _memoryPipe.Add(new HandlingProcessFor<IMessage>(message, () => model.BasicAck(ea.DeliveryTag, false), (ex) => OnError(ex, model, ea)));
                };

                var result = consumer.BasicConsume(queue: queueName, false, _eventConsumer);
            }
        }

        private void OnError(Exception ex, IModel model, BasicDeliverEventArgs ea)
        {
            _loggerFactory.CreateLogger<RabbitMQMessageReceiver>().LogCritical(ex, "An error occured.");
            model.BasicNack(ea.DeliveryTag, false, false);
        }
    }
}
