using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public class FaultConsumerFactory<TMessage, THandler> : ConsumerFactory<MassTransitFaultConsumer<TMessage>>
        where TMessage : class, IMessage
        where THandler : class, IFaultHandler<TMessage>
    {
        public FaultConsumerFactory(IServiceProvider services) : base(services) { }

        protected override MassTransitFaultConsumer<TMessage> GetConsumer(IServiceProvider services)
        {
            var handler = services.GetService<THandler>();

            if (handler == null)
            {
                throw new ConsumerException($"Unable to resolve fault handler type 'IFaultHandler<{typeof(TMessage).Name}>'.");                    
            }
            
            return new MassTransitFaultConsumer<TMessage>(handler);
        }
    }
}