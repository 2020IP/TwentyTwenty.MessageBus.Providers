using System.Threading.Tasks;
using MassTransit;
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

    public class MassTransitConsumer<TMessage, TResponse> : IConsumer<TMessage>
        where TMessage : class, IMessage
        where TResponse : class, IResponse
    {
        private readonly IHandle<TMessage, TResponse> _handler;

        public MassTransitConsumer(IHandle<TMessage, TResponse> handler)
        {
            _handler = handler;
        }

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            var response = await _handler.Handle(context.Message).ConfigureAwait(false);
            await context.RespondAsync(response).ConfigureAwait(false);
        }
    }
}
