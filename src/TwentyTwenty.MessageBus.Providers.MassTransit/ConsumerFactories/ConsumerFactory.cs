using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;

namespace TwentyTwenty.MessageBus.Providers.MassTransit.ConsumerFactories
{
    public abstract class ConsumerFactory<TConsumer> : IConsumerFactory<TConsumer>
        where TConsumer : class, IConsumer
    {
        private readonly IServiceProvider _services;

        public ConsumerFactory(IServiceProvider services)
        {
            _services = services;
        }

        async Task IConsumerFactory<TConsumer>.Send<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<TConsumer, T>> next)
        {
            var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();

            using(var scope = scopeFactory.CreateScope())
            {
                var consumer = GetConsumer(scope.ServiceProvider);
                
                var consumerConsumeContext = context.PushConsumer(consumer);

                await next.Send(consumerConsumeContext).ConfigureAwait(false);
            }
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            context.CreateConsumerFactoryScope<TConsumer>("ttcr");
        }

        protected abstract TConsumer GetConsumer(IServiceProvider services);
    }
}