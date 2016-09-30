using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitFaultConsumer<TMessage> : IConsumer<Fault<TMessage>>
        where TMessage : class, IMessage
    {
        private readonly IFaultHandler<TMessage> _handler;

        public MassTransitFaultConsumer(IFaultHandler<TMessage> handler)
        {
            _handler = handler;
        }

        public Task Consume(ConsumeContext<Fault<TMessage>> context)
        {
            var fault = new MessageFault<TMessage>
            {
                FaultId = context.Message.FaultId,
                MessageId = context.Message.FaultedMessageId,
                Message = context.Message.Message,
                Errors = context.Message.Exceptions?.Select(ex =>
                {
                    var nextEx = new Exception(ex.Message);
                    nextEx.Data.Add("MassTransitStackTrace", ex.StackTrace);
                    nextEx.Data.Add("MassTransitExceptionType", ex.ExceptionType);
                    return nextEx;
                }).ToArray(),
            };

            _handler.Handle(fault);

            return Task.FromResult(false);
        }
    }
}