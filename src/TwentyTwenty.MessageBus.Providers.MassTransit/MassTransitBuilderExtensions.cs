﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public static class MassTransitBuilderExtensions
    {
        public static void UseMassTransitFaultAutoRegistrar(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<MassTransitFaultBusAutoRegistrar>().RegisterHandlers();
        }

        public static void StartMassTransit(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<MassTransitMessageBus>().StartAsync().Wait();
        }
    }
}
