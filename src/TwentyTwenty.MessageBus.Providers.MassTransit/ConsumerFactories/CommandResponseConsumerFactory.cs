using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public class CommandResponseConsumerFactory<TMessage, TResponse> : ConsumerFactory<MassTransitConsumer<TMessage, TResponse>>
        where TMessage : class, ICommand
        where TResponse : class, IResponse
    {
        public CommandResponseConsumerFactory(IServiceProvider services) : base(services) {}

        protected override MassTransitConsumer<TMessage, TResponse> GetConsumer(IServiceProvider services)
        {
            var handler = services.GetService<ICommandHandler<TMessage, TResponse>>();

            if (handler == null)
            {
                throw new ConsumerException($"Unable to resolve fault handler type 'IFaultHandler<{typeof(TMessage).Name}>'.");                    
            }
            
            return new MassTransitConsumer<TMessage, TResponse>(handler);
        }
    }
}