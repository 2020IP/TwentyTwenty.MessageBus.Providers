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
                else if (registration.ServiceType.Closes(typeof(IDistributedEventListener<>)))
                {
                    return typeof(CachedDistributedEventListenerConfigurator<,>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType, registration.ImplementationType);
                }
                else if (registration.ServiceType.Closes(typeof(IEventListener<>)))
                {
                    return typeof(CachedEventListenerConfigurator<,>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType, registration.ImplementationType);
                }
                else if (registration.ServiceType.Closes(typeof(ICommandHandler<,>)))
                {
                    return typeof(CachedCommandResponseConfigurator<,>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType, registration.ResponseType);
                }
                else if (registration.ServiceType.Closes(typeof(IFaultHandler<>)))
                {
                    return typeof(CachedFaultConfigurator<,>).CloseAndBuildAs<CachedConfigurator>(registration.MessageType, registration.ImplementationType);
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

        class CachedFaultConfigurator<TMessage, THandler> : CachedConfigurator
            where TMessage : class, IMessage
            where THandler : class, IFaultHandler<TMessage>
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new FaultConsumerFactory<TMessage, THandler>(services));
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

        class CachedEventListenerConfigurator<TEvent, TListener> : CachedConfigurator
            where TEvent : class, IDomainEvent
            where TListener : class, IHandle<TEvent>
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new EventListenerConsumerFactory<TEvent, TListener>(services));
            }
        }

        class CachedDistributedEventListenerConfigurator<TEvent, TListener> : CachedConfigurator
            where TEvent : class, IDomainEvent
            where TListener : class, IHandle<TEvent>
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new DistributedEventListenerConsumerFactory<TEvent, TListener>(services));
            }
        }
    }
}