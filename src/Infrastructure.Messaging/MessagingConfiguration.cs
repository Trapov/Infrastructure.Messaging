namespace Infrastructure.Messaging
{
    using Microsoft.Extensions.DependencyInjection;
    using System.Text.Json;
    using System;

    public sealed class MessagingConfiguration
    {
        public MessagingConfiguration(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public MessagingConfiguration UseJsonPacker(
            Action<JsonSerializerOptions> jsonConfigurations)
        {
            var settings = new JsonSerializerOptions();
            jsonConfigurations(settings);
            Services.AddSingleton(settings);
            Services.AddSingleton<IMessagePacker, JsonMessagePacker>();
            return this;
        }
    }
}
