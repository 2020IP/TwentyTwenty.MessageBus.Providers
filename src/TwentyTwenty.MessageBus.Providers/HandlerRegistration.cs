using System;

namespace TwentyTwenty.MessageBus.Providers
{
    public class HandlerRegistration
    {
        public Type ImplementationType { get; set; }

        public Type ServiceType { get; set; }

        public Type MessageType { get; set; }
    }
}