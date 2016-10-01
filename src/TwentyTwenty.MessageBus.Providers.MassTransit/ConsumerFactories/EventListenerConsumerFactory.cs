using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public class EventListenerConsumerFactory<TMessage> : ConsumerFactory<MassTransitConsumer<TMessage>>
        where TMessage : class, IDomainEvent        
    {
        public EventListenerConsumerFactory(IServiceProvider services) : base(services) { }

        protected override MassTransitConsumer<TMessage> GetConsumer(IServiceProvider services)
        {
            var handler = services.GetService<IEventListener<TMessage>>();

            if (handler == null)
            {
                throw new ConsumerException($"Unable to resolve fault handler type 'IFaultHandler<{typeof(TMessage).Name}>'.");                    
            }
            
            return new MassTransitConsumer<TMessage>(handler);   
        }
    }
}