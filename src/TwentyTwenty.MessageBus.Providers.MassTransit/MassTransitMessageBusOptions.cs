using MassTransit;
using MassTransit.Builders;
using MassTransit.Pipeline;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitMessageBusOptions
    {
        public IBusFactorySpecification BusObserver { get; set; }

        public IReceiveObserver ReceiveObserver { get; set; }
        
        public ISendObserver SendObserver { get; set; }

        public IConsumeObserver ConsumeObserver { get; set; }

        public IPublishObserver PublishObserver { get; set; }

        public IRetryPolicy RetryPolicy { get; set; } = Retry.Immediate(5);

        public bool UseInMemoryBus { get; set; }

        public string RabbitMQUri { get; set; }

        public string RabbitMQUsername { get; set; }

        public string RabbitMQPassword { get; set; }
    }
}