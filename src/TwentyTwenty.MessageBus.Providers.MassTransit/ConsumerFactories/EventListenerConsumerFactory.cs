using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public class EventListenerConsumerFactory<TMessage, TListener> : ConsumerFactory<MassTransitConsumer<TMessage>>
        where TMessage : class, IDomainEvent
        where TListener : class, IHandle<TMessage>
    {
        public EventListenerConsumerFactory(IServiceProvider services) : base(services) { }

        protected override MassTransitConsumer<TMessage> GetConsumer(IServiceProvider services)
        {
            var handler = services.GetService<TListener>();

            if (handler == null)
            {
                throw new ConsumerException($"Unable to resolve event listener type 'IEventListener<{typeof(TMessage).Name}>'.");                    
            }
            
            return new MassTransitConsumer<TMessage>(handler);   
        }
    }
}