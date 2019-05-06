namespace Infrastructure.Messaging.RabbitMQ
{
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class RabbitMQMessagePublisher : IMessagePublisher
    {
        private readonly Lazy<IConnection> _connection;
        private readonly Lazy<IModel> _publishChannel;
        private readonly TaskFactory _taskFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessagePacker _messagePacker;

        public RabbitMQMessagePublisher(
            ILoggerFactory loggerFactory,
            TaskFactory taskFactory,
            IMessagePacker messagePacker,
            IConnectionFactory connectionFactory)
        {
            _taskFactory = taskFactory;
            _messagePacker = messagePacker;
            _loggerFactory = loggerFactory;
            _connection = _connection = new Lazy<IConnection>(() =>
            {
                var logger = _loggerFactory.CreateLogger<RabbitMQMessagePublisher>();

                var tryCount = 0;
                var timeOut = TimeSpan.FromSeconds(4);

                while (tryCount != 20)
                {
                    Interlocked.Increment(ref tryCount);
                    try
                    {
                        var connection = connectionFactory.CreateConnection(Assembly.GetEntryAssembly().FullName);
                        logger.LogInformation("Connection is succesfull => {connection}", connection);
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
            _publishChannel = new Lazy<IModel>(() => _connection.Value.CreateModel());
        }

        public Task Publish(IMessage message, CancellationToken cancellationToken)
        {
            var eventType = message.GetType();
            var exchangeName = eventType.Namespace.Split('.').First(); //TODO: exchangeName 
            var routingKey = eventType.Name;
            var queueName = $"{exchangeName}.{routingKey}";

            _publishChannel.Value.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);
            _publishChannel.Value.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            _publishChannel.Value.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);

            return _taskFactory.StartNew(() =>
            {
                var packedMessage = (string)_messagePacker.Pack(message, cancellationToken).GetAwaiter().GetResult();
                _publishChannel.Value.BasicPublish(exchange: exchangeName, routingKey: routingKey, body: Encoding.UTF8.GetBytes(packedMessage));
            }, cancellationToken);
        }
    }
}
