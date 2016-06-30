using System;
using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public interface IFaultHandlerRegistrar
    {
        void RegisterHandler<T>(Action<MessageFault<T>> handler) where T : class, IMessage;
    }
}