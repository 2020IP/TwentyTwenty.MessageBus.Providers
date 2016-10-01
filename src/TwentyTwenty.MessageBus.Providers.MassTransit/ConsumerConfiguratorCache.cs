using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MassTransit;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public static class ConsumerConfiguratorCache
    {
        private  static readonly ConcurrentDictionary<Type, CachedConfigurator> Instance =
                new ConcurrentDictionary<Type, CachedConfigurator>();

        static CachedConfigurator GetOrAdd(HandlerRegistration registration)
        {
            return Instance.GetOrAdd(registration.ImplementationType, _ =>
            {
                if (registration.ServiceType.Closes(typeof(ICommandHandler<>)))
                {
                    return typeof(CachedCommandConfigurator<>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType);
                }
                else if (registration.ServiceType.Closes(typeof(IEventListener<>)))
                {
                    return typeof(CachedEventListenerConfigurator<>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType);
                }
                else if (registration.ServiceType.Closes(typeof(ICommandHandler<,>)))
                {
                    return typeof(CachedCommandResponseConfigurator<,>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType, registration.ResponseType);
                }
                else if (registration.ServiceType.Closes(typeof(IFaultHandler<>)))
                {
                    return typeof(CachedFaultConfigurator<>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType);
                }

                throw new InvalidOperationException($"Unable to find Configurator for service type `{registration.ServiceType.Name}`");
            });
        }

        public static void Configure(HandlerRegistration registration, IReceiveEndpointConfigurator configurator, IServiceProvider container)
        {
            GetOrAdd(registration).Configure(configurator, container);
        }

        public static void Cache(HandlerRegistration registration)
        {
            GetOrAdd(registration);
        }

        public static IEnumerable<Type> GetConsumers()
        {
            return Instance.Keys;
        }        

        interface CachedConfigurator
        {
            void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services);
        }

        class CachedFaultConfigurator<T> : CachedConfigurator
            where T : class, IMessage
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new FaultConsumerFactory<T>(services));
            }
        }

        class CachedCommandConfigurator<TCommand> : CachedConfigurator
            where TCommand : class, ICommand
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new CommandConsumerFactory<TCommand>(services));
            }
        }

        class CachedCommandResponseConfigurator<TMessage, TResponse> : CachedConfigurator            
            where TMessage : class, ICommand
            where TResponse : class, IResponse
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new CommandResponseConsumerFactory<TMessage, TResponse>(services));
            }
        }

        class CachedEventListenerConfigurator<TEvent> : CachedConfigurator
            where TEvent : class, IDomainEvent
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new EventListenerConsumerFactory<TEvent>(services));
            }
        }
    }
}