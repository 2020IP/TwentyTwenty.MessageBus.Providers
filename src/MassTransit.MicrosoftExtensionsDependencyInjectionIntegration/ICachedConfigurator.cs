
using System;

namespace MassTransit.MicrosoftExtensionsDependencyInjectionIntegration
{
    internal interface ICachedConfigurator
    {
        void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services);
    }
}