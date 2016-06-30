using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;

namespace TwentyTwenty.MessageBus.Providers
{
    public static class MessageBusServiceExtensions
    {
        public static void AddMessageBusHandlers(this IServiceCollection services, Assembly commandHandlerAssembly, Assembly eventListenerAssembly)
        {
            var commandHandlers = services.AddCommandHandlers(commandHandlerAssembly);
            var eventListeners = services.AddEventListeners(eventListenerAssembly);

            services.AddSingleton(s => new BusAutoRegistrar(s, s.GetRequiredService<IHandlerRegistrar>(), commandHandlers, eventListeners));
        }

        private static HandlerRegistration[] AddCommandHandlers(this IServiceCollection services, Assembly asm)
        {
            return asm.GetTypes()
                .Select(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
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

        private static HandlerRegistration[] AddEventListeners(this IServiceCollection services, Assembly asm)
        {
            return asm.GetTypes()
                .Select(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventListener<>))
                    .Select(i => new HandlerRegistration
                    {
                        ImplementationType = t,
                        ServiceType = i,
                        MessageType = i.GetGenericArguments().First(),
                    })
                    .FirstOrDefault())
                .Where(l => l != null)
                .Select(l =>
                {
                    services.AddScoped(l.ServiceType, l.ImplementationType);
                    return l;
                })
                .ToArray();
        }
    }
}