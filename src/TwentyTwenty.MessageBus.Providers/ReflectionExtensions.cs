using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TwentyTwenty.MessageBus.Providers
{
    public static class ReflectionExtensions
    {
        public static bool IsOpenGeneric(this Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition || type.GetTypeInfo().ContainsGenericParameters;
        }

        public static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
        {
            var openInterface = closedInterface.GetGenericTypeDefinition();
            var arguments = closedInterface.GenericTypeArguments;

            var concreteArguments = openConcretion.GenericTypeArguments;
            return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
        }

        public static bool CanBeCastTo(this Type handlerType, Type interfaceType)
        {
            if (handlerType == null) return false;

            if (handlerType == interfaceType) return true;

            return interfaceType.GetTypeInfo().IsAssignableFrom(handlerType.GetTypeInfo());
        }

        public static bool CanBeCastTo<T>(this Type originalType)
        {
            return originalType.CanBeCastTo(typeof(T));
        }

        public static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
        {
            if (!pluggedType.IsConcrete()) yield break;

            if (templateType.GetTypeInfo().IsInterface)
            {
                foreach (
                    var interfaceType in
                        pluggedType.GetTypeInfo().ImplementedInterfaces
                            .Where(type => type.GetTypeInfo().IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
                {
                    yield return interfaceType;
                }
            }
            else if (pluggedType.GetTypeInfo().BaseType.GetTypeInfo().IsGenericType &&
                     (pluggedType.GetTypeInfo().BaseType.GetGenericTypeDefinition() == templateType))
            {
                yield return pluggedType.GetTypeInfo().BaseType;
            }

            if (pluggedType.GetTypeInfo().BaseType == typeof(object)) 
            {
                yield break;
            }

            foreach (var interfaceType in FindInterfacesThatClose(pluggedType.GetTypeInfo().BaseType, templateType))
            {
                yield return interfaceType;
            }
        }

        public static bool IsConcrete(this Type type)
        {
            return !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface;
        }

        public static IList<HandlerRegistration> FindHandlers(this IEnumerable<Assembly> assembliesToScan, params Type[] handlerTypes)
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

            return registrations;
        }
    }
}