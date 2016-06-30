using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public interface IFaultHandler<T> where T : class, IMessage
    {
        void Handle(MessageFault<T> message);
    }
}