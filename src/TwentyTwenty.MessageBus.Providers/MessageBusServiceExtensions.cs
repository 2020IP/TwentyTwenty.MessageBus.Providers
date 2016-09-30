using System;
using System.Linq;
using System.Reflection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using System.Collections.Generic;
using TwentyTwenty.MessageBus.Providers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessageBusExtensions
    {
        public static void AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var manager = StaticHelpers.GetHandlerManager(services);
            var handlers = assemblies.FindHandlers(typeof(ICommandHandler<>), typeof(ICommandHandler<,>));
            
            foreach (var handler in handlers)
            {
                manager.CommandHandlers.Add(handler);
                services.AddScoped(handler.ImplementationType, handler.ImplementationType);
            }
        }

        public static void AddEventListeners(this IServiceCollection services, params Assembly[] assemblies)
        {
            var manager = StaticHelpers.GetHandlerManager(services);
            var listeners = assemblies.FindHandlers(typeof(IEventListener<>));

            foreach (var listener in listeners)
            {
                manager.EventListeners.Add(listener);
                services.AddScoped(listener.ImplementationType, listener.ImplementationType);
            }
        }

        public static void AddFaultHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var manager = StaticHelpers.GetHandlerManager(services);
            var listeners = assemblies.FindHandlers(typeof(IFaultHandler<>));

            foreach (var listener in listeners)
            {
                manager.EventListeners.Add(listener);
                services.AddScoped(listener.ServiceType, listener.ImplementationType);
            }
        }
    }
}