using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class FaultConsumerFactory<TMessage> : IConsumerFactory<MassTransitFaultConsumer<TMessage>>
        where TMessage : class, IMessage
    {
        private readonly IServiceProvider _services;

        public FaultConsumerFactory(IServiceProvider services)
        {
            _services = services;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            context.CreateConsumerFactoryScope<MassTransitFaultConsumer<TMessage>>("ttfh");
        }

        async Task IConsumerFactory<MassTransitFaultConsumer<TMessage>>.Send<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<MassTransitFaultConsumer<TMessage>, T>> next)
        {
            var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();

            using(var scope = scopeFactory.CreateScope())
            {
                var handler = scope.ServiceProvider.GetService<IFaultHandler<TMessage>>();
                if (handler == null)
                {
                    throw new ConsumerException($"Unable to resolve fault handler type 'IFaultHandler<{typeof(TMessage).Name}>'.");                    
                }
                
                var consumer = new MassTransitFaultConsumer<TMessage>(handler);
                
                var consumerConsumeContext = context.PushConsumer(consumer);

                await next.Send(consumerConsumeContext).ConfigureAwait(false);
            }
        }
    }
}