using MassTransit.Builders;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitMessageBusOptions
    {
        public IBusFactorySpecification BusObserver { get; set; }

        public bool UseInMemoryBus { get; set; }

        public string RabbitMQUri { get; set; }

        public string RabbitMQUsername { get; set; }

        public string RabbitMQPassword { get; set; }
    }
}