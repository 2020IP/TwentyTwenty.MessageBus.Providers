using System;
using System.Linq;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Logging;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using TwentyTwenty.MessageBus.Providers;
using TwentyTwenty.MessageBus.Providers.MassTransit;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MassTransitServiceExtensions
    {
        public static void AddMassTransitMessageBus(this IServiceCollection services, MassTransitMessageBusOptions options)
        {
            if (options == null)
            {
                throw new ArgumentException(nameof(options));
            }

            // Force the adding of the HandlerManager
            var manager = StaticHelpers.GetHandlerManager(services);

            services.AddSingleton(s => new MassTransitMessageBus(options, s.GetRequiredService<HandlerManager>(), s, s.GetRequiredService<ILoggerFactory>()));
            services.AddSingleton<ICommandSender>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<ICommandSenderReceiver>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IEventPublisher>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IHandlerRegistrar>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IHandlerRequestResponseRegistrar>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IFaultHandlerRegistrar>(s => s.GetService<MassTransitMessageBus>());
        }
    }
}