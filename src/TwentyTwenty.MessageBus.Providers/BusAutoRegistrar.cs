using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using System.Threading.Tasks;

namespace TwentyTwenty.MessageBus.Providers
{
    public class BusAutoRegistrar
    {
        private IServiceProvider _services;
        private IHandlerRegistrar _registrar;
        private IHandlerRequestResponseRegistrar _responseRegistrar;
        private HandlerRegistration[] _commandHandlers, _responseCommandHandlers, _eventListeners;

        public BusAutoRegistrar(
            IServiceProvider services, 
            IHandlerRegistrar registrar,
            IHandlerRequestResponseRegistrar responseRegistrar,
            HandlerRegistration[] commandHandlers,
            HandlerRegistration[] responseCommandHandlers,
            HandlerRegistration[] eventListeners)
        {
            _services = services;
            _registrar = registrar;
            _responseRegistrar = responseRegistrar;
            _commandHandlers = commandHandlers;
            _responseCommandHandlers = responseCommandHandlers;
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

            method = typeof(BusAutoRegistrar).GetMethod("RegisterResponseCommandHandler");

            foreach (var handler in _responseCommandHandlers)
            {
                method.MakeGenericMethod(handler.MessageType, handler.ResponseType)
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
            _registrar.RegisterHandler<T>(async msg =>
            {
                await _services.GetService<ICommandHandler<T>>().Handle(msg);
            });
        }

        public void RegisterResponseCommandHandler<T, TResult>() 
            where T : class, ICommand 
            where TResult : class, IResponse
        {
            _responseRegistrar.RegisterHandler<T, TResult>(msg =>
            {
                return _services.GetService<ICommandHandler<T, TResult>>().Handle(msg);
            });
        }

        public void RegisterEventListener<T>() where T : class, IDomainEvent
        {
            _registrar.RegisterHandler<T>(async msg =>
            {
                await _services.GetService<IEventListener<T>>().Handle(msg);
            });
        }
    }
}