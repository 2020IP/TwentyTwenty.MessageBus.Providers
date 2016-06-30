using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TwentyTwenty.MessageBus.Providers
{
    public static class MessageBusBuilderExtensions
    {
        public static void UseMessageBusAutoRegistrar(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<BusAutoRegistrar>().RegisterHandlers();
        }
    }
}