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
        // public static void AddMessageBusHandlers(this IServiceCollection services, Assembly commandHandlerAssembly, Assembly eventListenerAssembly)
        // {
        //     if (commandHandlerAssembly == null)
        //     {
        //         throw new ArgumentException("The command handler assembly cannot be null.");
        //     }

        //     if (eventListenerAssembly == null)
        //     {
        //         throw new ArgumentException("The event listener assembly cannot be null.");
        //     }

            // var commandHandlers = services.AddCommandHandlers(commandHandlerAssembly);
            // var responesCommandHandlers = services.AddResponseCommandHandlers(commandHandlerAssembly);
            // var eventListeners = services.AddEventListeners(eventListenerAssembly);

            // services.AddSingleton(s => new BusAutoRegistrar(s, s.GetRequiredService<IHandlerRegistrar>(), 
            //     s.GetRequiredService<IHandlerRequestResponseRegistrar>(), commandHandlers, responesCommandHandlers, eventListeners));
        // }

        public static void AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var manager = StaticHelpers.GetHandlerManager(services);
            var handlers = AddHandlers(services, assemblies, typeof(ICommandHandler<>), typeof(ICommandHandler<,>));
            
            foreach (var handler in handlers)
            {
                manager.CommandHandlers.Add(handler);
            }
        }

        public static void AddEventListeners(this IServiceCollection services, params Assembly[] assemblies)
        {
            var manager = StaticHelpers.GetHandlerManager(services);
            var listeners = AddHandlers(services, assemblies, typeof(IEventListener<>));

            foreach (var listener in listeners)
            {
                manager.EventListeners.Add(listener);
            }
        }

        private static HandlerRegistration[] AddResponseCommandHandlers(this IServiceCollection services, Assembly asm)
        {
            return asm.GetTypes()
                .Select(t => t.GetInterfaces()
                    .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                    .Select(i => new HandlerRegistration
                    {
                        ImplementationType = t,
                        ServiceType = i,
                        MessageType = i.GetGenericArguments().First(),
                        ResponseType = i.GetGenericArguments().Last(),
                    })
                    .FirstOrDefault())
                .Where(h => h != null)
                .Select(h =>
                {
                    services.AddTransient(h.ServiceType, h.ImplementationType);
                    return h;
                })
                .ToArray();
        }

        // private static HandlerRegistration[] AddCommandHandlers(this IServiceCollection services, Assembly asm)
        // {
        //     return asm.GetTypes()
        //         .Select(t => t.GetInterfaces()
        //             .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
        //             .Select(i => new HandlerRegistration
        //             {
        //                 ImplementationType = t,
        //                 ServiceType = i,
        //                 MessageType = i.GetGenericArguments().First(),
        //             })
        //             .FirstOrDefault())
        //         .Where(h => h != null)
        //         .Select(h =>
        //         {
        //             services.AddScoped(h.ServiceType, h.ImplementationType);
        //             return h;
        //         })
        //         .ToArray();
        // }

        // private static HandlerRegistration[] AddEventListeners(this IServiceCollection services, Assembly asm)
        // {
        //     return asm.GetTypes()
        //         .Select(t => t.GetInterfaces()
        //             .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventListener<>))
        //             .Select(i => new HandlerRegistration
        //             {
        //                 ImplementationType = t,
        //                 ServiceType = i,
        //                 MessageType = i.GetGenericArguments().First(),
        //             })
        //             .FirstOrDefault())
        //         .Where(l => l != null)
        //         .Select(l =>
        //         {
        //             services.AddScoped(l.ServiceType, l.ImplementationType);
        //             return l;
        //         })
        //         .ToArray();
        // }

        private static IList<HandlerRegistration> AddHandlers(this IServiceCollection services, IEnumerable<Assembly> assembliesToScan, params Type[] handlerTypes)
        {
            assembliesToScan = assembliesToScan as Assembly[] ?? assembliesToScan.ToArray();
            
            var concretions = new List<Type>();
            var interfaces = new HashSet<Type>();

            foreach (var type in assembliesToScan.SelectMany(a => a.ExportedTypes))
            {
                var interfaceTypes = handlerTypes.SelectMany(h => type.FindInterfacesThatClose(h)).ToArray();
                
                if (!interfaceTypes.Any())
                { 
                    continue;
                }

                if (type.IsConcrete())
                {
                    concretions.Add(type);
                }

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaces.Add(interfaceType);
                }
            }

            var registrations = interfaces
                .Select(i => new { Interface = i, Matches = concretions.Where(t => t.CanBeCastTo(i)).ToArray() })
                .SelectMany(i => i.Matches.Select(m => new HandlerRegistration
                {
                    ImplementationType = m,
                    ServiceType = i.Interface,
                    MessageType = i.Interface.GetGenericArguments().FirstOrDefault(),
                    ResponseType = i.Interface.GetGenericArguments().Skip(1).FirstOrDefault(),
                }))
                .ToArray();
            
            foreach(var registration in registrations)
            {
                services.AddScoped(registration.ServiceType, registration.ImplementationType);
            }

            return registrations;
        }
    }
}