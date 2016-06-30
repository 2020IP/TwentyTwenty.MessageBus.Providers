using System;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MessageFault<T> where T : class, IMessage
    {
        public Guid FaultId { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public T Message { get; set; }
    }
}