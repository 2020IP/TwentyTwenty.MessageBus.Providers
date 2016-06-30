using System;
using TwentyTwenty.DomainDriven;
using Microsoft.Extensions.DependencyInjection;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitFaultBusAutoRegistrar
    {
        private IServiceProvider _services;
        private IFaultHandlerRegistrar _faultRegistrar;
        private HandlerRegistration[]  _faultHandlers;

        public MassTransitFaultBusAutoRegistrar(
            IServiceProvider services, 
            IFaultHandlerRegistrar faultRegistrar, 
            HandlerRegistration[] faultHandlers)
        {
            _services = services;
            _faultRegistrar = faultRegistrar;
            _faultHandlers = faultHandlers;
        }

        public void RegisterHandlers()
        {
            // UGH, too much reflection.  Is there a better way?
            var method = typeof(BusAutoRegistrar).GetMethod("RegisterFaultHandler");

            foreach (var handler in _faultHandlers)
            {
                method.MakeGenericMethod(handler.MessageType)
                    .Invoke(this, null);
            }
        }

        public void RegisterFaultHandler<T>() where T : class, IMessage
        {
            _faultRegistrar.RegisterHandler<T>(msg =>
            {
                _services.GetService<IFaultHandler<T>>().Handle(msg);
            });
        }
    }
}