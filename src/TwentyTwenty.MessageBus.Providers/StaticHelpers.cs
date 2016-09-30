using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace TwentyTwenty.MessageBus.Providers
{
    public static class StaticHelpers
    {
        public static HandlerManager GetHandlerManager(IServiceCollection services)
        {
            var manager = (HandlerManager)services
                .FirstOrDefault(d => d.ServiceType == typeof(HandlerManager))
                ?.ImplementationInstance;
            
            if (manager == null)
            {
                manager = new HandlerManager();
                services.AddSingleton(manager);
            }
            
            return manager;
        }
    }
}