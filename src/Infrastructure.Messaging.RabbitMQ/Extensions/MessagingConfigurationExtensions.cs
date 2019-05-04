namespace Infrastructure.Messaging.RabbitMQ.Extensions
{
    using global::RabbitMQ.Client;
    using Microsoft.Extensions.DependencyInjection;
    using System;

    public static class MessagingConfigurationExtensions
    {
        public static MessagingConfiguration UseRabbitMQ(
            this MessagingConfiguration messagingConfiguration,
            Action<IConnectionFactory> connectionFactoryBuilder)
        {
            var connectionFactory = new ConnectionFactory();
            connectionFactoryBuilder(connectionFactory);

            messagingConfiguration
                .Services
                    .AddSingleton<IConnectionFactory>(connectionFactory)
                    .AddSingleton<IMessageReceiver, RabbitMQMessageReceiver>()
                    .AddSingleton<IMessagePublisher, RabbitMQMessagePublisher>();

            return messagingConfiguration;
        }
    }
}
