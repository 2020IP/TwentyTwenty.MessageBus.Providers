using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public static class MassTransitBuilderExtensions
    {
        public static void StartMassTransit(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<MassTransitMessageBus>().StartAsync().Wait();
        }

        public static void StopMassTransit(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<MassTransitMessageBus>().StopAsync().Wait();
        }
    }
}
