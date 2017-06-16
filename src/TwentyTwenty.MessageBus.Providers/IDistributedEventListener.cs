using TwentyTwenty.DomainDriven;

namespace TwentyTwenty.MessageBus.Providers
{
    public interface IDistributedEventListener<in T> : IHandle<T> where T : IDomainEvent
    {
    }
}