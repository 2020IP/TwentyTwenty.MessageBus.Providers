using System;
using System.Collections.Generic;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers
{
    public class MessageFault<T> where T : class, IMessage
    {
        public Guid FaultId { get; set; }

        public Guid? MessageId { get; set; }

        public IList<Exception> Errors { get; set; }

        public T Message { get; set; }
    }
}