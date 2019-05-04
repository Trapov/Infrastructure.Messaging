namespace Infrastructure.Messaging
{
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using System;

    public sealed class MessagingConfiguration
    {
        public MessagingConfiguration(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public MessagingConfiguration UseJsonPacker(
            Action<JsonSerializerSettings> jsonConfigurations)
        {
            var settings = new JsonSerializerSettings();
            jsonConfigurations(settings);
            JsonConvert.DefaultSettings = () => settings;
            Services.AddSingleton<IMessagePacker, JsonMessagePacker>();
            return this;
        }

        public MessagingConfiguration UseIoC()
        {
            Services.AddSingleton<IMessageHandlersRegistry, MessageHandlersRegistryIoC>();
            return this;
        }
    }
}
