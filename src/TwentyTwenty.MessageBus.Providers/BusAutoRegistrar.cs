using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;

namespace TwentyTwenty.MessageBus.Providers
{
    public class BusAutoRegistrar
    {
        private IServiceProvider _services;
        private IHandlerRegistrar _registrar;
        private HandlerRegistration[] _commandHandlers, _eventListeners;

        public BusAutoRegistrar(
            IServiceProvider services, 
            IHandlerRegistrar registrar, 
            HandlerRegistration[] commandHandlers, 
            HandlerRegistration[] eventListeners)
        {
            _services = services;
            _registrar = registrar;
            _commandHandlers = commandHandlers;
            _eventListeners = eventListeners;
        }

        public void RegisterHandlers()
        {
            // UGH, too much reflection.  Is there a better way?

            var method = typeof(BusAutoRegistrar).GetMethod("RegisterCommandHandler");
            
            foreach (var handler in _commandHandlers)
            {
                method.MakeGenericMethod(handler.MessageType)
                    .Invoke(this, null);
            }

            method = typeof(BusAutoRegistrar).GetMethod("RegisterEventListener");

            foreach (var handler in _eventListeners)
            {
                method.MakeGenericMethod(handler.MessageType)
                    .Invoke(this, null);
            }
        }

        public void RegisterCommandHandler<T>() where T : class, ICommand
        {
            _registrar.RegisterHandler<T>(msg =>
            {
                _services.GetService<ICommandHandler<T>>().Handle(msg);
            });
        }

        public void RegisterEventListener<T>() where T : class, IDomainEvent
        {
            _registrar.RegisterHandler<T>(msg =>
            {
                _services.GetService<IEventListener<T>>().Handle(msg);
            });
        }
    }
}