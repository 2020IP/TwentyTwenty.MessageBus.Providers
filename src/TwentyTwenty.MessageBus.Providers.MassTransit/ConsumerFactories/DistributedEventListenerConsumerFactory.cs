using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public class DistributedEventListenerConsumerFactory<TMessage, TListener> : ConsumerFactory<MassTransitConsumer<TMessage>>
        where TMessage : class, IDomainEvent
        where TListener : class, IHandle<TMessage>
    {
        public DistributedEventListenerConsumerFactory(IServiceProvider services) : base(services) { }

        protected override MassTransitConsumer<TMessage> GetConsumer(IServiceProvider services)
        {
            var handler = services.GetService<TListener>();

            if (handler == null)
            {
                throw new ConsumerException($"Unable to resolve distributed event listener type 'IDistributedEventListener<{typeof(TMessage).Name}>'.");                    
            }
            
            return new MassTransitConsumer<TMessage>(handler);   
        }
    }
}