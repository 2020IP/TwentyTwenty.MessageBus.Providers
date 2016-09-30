using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers
{
    public interface IFaultHandler<T> where T : class, IMessage
    {
        void Handle(MessageFault<T> message);
    }
}