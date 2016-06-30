using MassTransit.BusConfigurators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitMessageBusOptions
    {
        public BusObserverSpecification BusObserver { get; set; }

        public bool UseInMemoryBus { get; set; }

        public string RabbitMQUri { get; set; }

        public string RabbitMQUsername { get; set; }

        public string RabbitMQPassword { get; set; }
    }
}