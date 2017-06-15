using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven.CQRS;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public class CommandConsumerFactory<TMessage> : ConsumerFactory<MassTransitConsumer<TMessage>>
        where TMessage : class, ICommand        
    {
        public CommandConsumerFactory(IServiceProvider services) : base(services) { }

        protected override MassTransitConsumer<TMessage> GetConsumer(IServiceProvider services)
        {
            var handler = services.GetService<ICommandHandler<TMessage>>();

            if (handler == null)
            {
                throw new ConsumerException($"Unable to resolve command handler type 'ICommandHandler<{typeof(TMessage).Name}>'.");                    
            }
            
            return new MassTransitConsumer<TMessage>(handler);   
        }
    }
}