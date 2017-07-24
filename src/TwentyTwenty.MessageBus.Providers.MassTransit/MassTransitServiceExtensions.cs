using System;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Logging;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using TwentyTwenty.MessageBus.Providers;
using TwentyTwenty.MessageBus.Providers.MassTransit;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MassTransitServiceExtensions
    {
        public static void AddMassTransitMessageBus(this IServiceCollection services, MassTransitMessageBusOptions options,
             Action<IRabbitMqBusFactoryConfigurator> configure = null)
        {
            if (options == null)
            {
                throw new ArgumentException(nameof(options));
            }

            // Force the adding of the HandlerManager
            StaticHelpers.GetHandlerManager(services);

            services.AddSingleton(s => new MassTransitMessageBus(options, s.GetRequiredService<HandlerManager>(), s, configure, s.GetRequiredService<ILoggerFactory>()));
            services.AddSingleton<ICommandSender>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<ICommandSenderReceiver>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IEventPublisher>(s => s.GetService<MassTransitMessageBus>());
        }
    }
}