using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public static class MassTransitBuilderExtensions
    {
        public static void UseMassTransitRegistry(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<BusAutoRegistrar>().RegisterHandlers();
        }
    }
}
