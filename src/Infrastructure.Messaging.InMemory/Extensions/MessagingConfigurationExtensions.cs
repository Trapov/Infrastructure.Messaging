namespace Infrastructure.Messaging.InMemory.Extensions
{
    using Microsoft.Extensions.DependencyInjection;
    public static class MessagingConfigurationExtensions
    {
        public static MessagingConfiguration UseInMemory(this MessagingConfiguration messagingConfiguration)
        {

            messagingConfiguration
                .Services
                    .AddSingleton<IMessageReceiver, InMemoryMessageReceiver>()
                    .AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();

            return messagingConfiguration;
        }
    }
}
