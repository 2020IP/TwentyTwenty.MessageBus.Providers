using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public static class MassTransitServiceExtensions
    {
        public static void AddMassTransitMessageBus(this IServiceCollection services, MassTransitMessageBusOptions options)
        {
            if (options == null)
            {
                throw new ArgumentException(nameof(options));
            }

            services.AddSingleton(s => new MassTransitMessageBus(options, s.GetRequiredService<HandlerManager>(), s));
            services.AddSingleton<ICommandSender>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IEventPublisher>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IHandlerRegistrar>(s => s.GetService<MassTransitMessageBus>());
            services.AddSingleton<IFaultHandlerRegistrar>(s => s.GetService<MassTransitMessageBus>());
        }

        public static void AddMasstTransitFaultHandlers(this IServiceCollection services, Assembly faultHandlerAssembly)
        {
            if (faultHandlerAssembly == null)
            {
                throw new ArgumentException("The fault handler assembly cannot be null.");
            }

            var faultHandlers = services.AddFaultHandlers(faultHandlerAssembly);

            services.AddSingleton(s => new MassTransitFaultBusAutoRegistrar(s, s.GetRequiredService<IFaultHandlerRegistrar>(), faultHandlers));
        }

        private static HandlerRegistration[] AddFaultHandlers(this IServiceCollection services, Assembly asm)
        {
            return asm.GetTypes()
                .Select(t => t.GetInterfaces()
                    .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IFaultHandler<>))
                    .Select(i => new HandlerRegistration
                    {
                        ImplementationType = t,
                        ServiceType = i,
                        MessageType = i.GetGenericArguments().First(),
                    })
                    .FirstOrDefault())
                .Where(h => h != null)
                .Select(h =>
                {
                    services.AddScoped(h.ServiceType, h.ImplementationType);
                    return h;
                })
                .ToArray();
        }
    }
}