using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitConsumer<TMessage> : IConsumer<TMessage>
        where TMessage : class, IMessage
    {
        private readonly IHandle<TMessage> _handler;

        public MassTransitConsumer(IHandle<TMessage> handler)
        {
            _handler = handler;
        }

        public Task Consume(ConsumeContext<TMessage> context)
        {
            return _handler.Handle(context.Message);
        }
    }
}
