using System.Collections.Generic;
using System.Linq;

namespace TwentyTwenty.MessageBus.Providers
{
    public class HandlerManager
    {
        public IList<HandlerRegistration> CommandHandlers { get; } = new List<HandlerRegistration>();

        public IList<HandlerRegistration> EventListeners { get; } = new List<HandlerRegistration>();

        public IList<HandlerRegistration> FaultHandlers { get; } = new List<HandlerRegistration>();

        public IEnumerable<HandlerRegistration> GetAllHandlers() 
            => CommandHandlers.Concat(EventListeners).Concat(FaultHandlers);
    }
}