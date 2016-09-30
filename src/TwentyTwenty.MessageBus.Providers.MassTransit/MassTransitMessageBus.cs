using System;
using System.Threading.Tasks;
using MassTransit;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using System.Collections.Generic;
using MassTransit.ConsumeConfigurators;
using MassTransit.MicrosoftExtensionsDependencyInjectionIntegration;
using System.Linq;
using System.Threading;
using System.Reflection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitMessageBus : IEventPublisher, ICommandSender, ICommandSenderReceiver, 
        IHandlerRegistrar, IHandlerRequestResponseRegistrar, IFaultHandlerRegistrar
    {
        private readonly ILogger<MassTransitMessageBus> _logger;
        private readonly Dictionary<string, List<IReceiveEndpointSpecification>> _handlers = 
            new Dictionary<string, List<IReceiveEndpointSpecification>>();
        private readonly MassTransitMessageBusOptions _options;
        private readonly HandlerManager _manager;
        private readonly IServiceProvider _services;
        private IBusControl _busControl = null;

        public MassTransitMessageBus(MassTransitMessageBusOptions options, HandlerManager manager, IServiceProvider services, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<MassTransitMessageBus>();
            _options = options;
            _manager = manager;
            _services = services;
        }

        public virtual async Task<TResult> Send<T, TResult>(T command) 
            where T : class, ICommand
            where TResult : class, IResponse
        {
            Uri endpoint;
            if (_options.UseInMemoryBus)
            {
                endpoint = new Uri($"loopback://localhost/{command.GetType().Name}");
            }
            else
            {
                endpoint = new Uri($"{_options.RabbitMQUri}/{command.GetType().Name}");
            }

            var createObject = typeof(MessageRequestClient<,>);
            var createGeneric = createObject.MakeGenericType(new Type[] { command.GetType(), typeof(TResult) });
            var createInstance = Activator.CreateInstance(createGeneric, new object[] { _busControl, endpoint, TimeSpan.FromSeconds(30), default(TimeSpan?), null });
            var requestMethod = createInstance.GetType().GetMethod("Request");
            var response = (dynamic)requestMethod.Invoke(createInstance, new object[] { command, new CancellationToken() });
            return await response;
        }

        public virtual async Task<TResult> Send<TResult>(ICommand command, Type commandType) 
            where TResult : class, IResponse
        {
            Uri endpoint;
            if (_options.UseInMemoryBus)
            {
                endpoint = new Uri($"loopback://localhost/{commandType.Name}");
            }
            else
            {
                endpoint = new Uri($"{_options.RabbitMQUri}/{commandType.Name}");
            }

            var createObject = typeof(MessageRequestClient<,>);
            var createGeneric = createObject.MakeGenericType(new Type[] { commandType, typeof(TResult) });
            var createInstance = Activator.CreateInstance(createGeneric, new object[] { _busControl, endpoint, TimeSpan.FromSeconds(30), default(TimeSpan?), null });
            var requestMethod = createInstance.GetType().GetMethod("Request");
            var response = (dynamic)requestMethod.Invoke(createInstance, new object[] { command, new CancellationToken() });
            return await response;
        }

        public virtual async Task Send<T>(T command) where T : class, ICommand
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before sending commands.");
            }

            ISendEndpoint endpoint;
            if (_options.UseInMemoryBus)
            {
                endpoint = await _busControl.GetSendEndpoint(
                    new Uri($"loopback://localhost/{command.GetType().Name}"))
                    .ConfigureAwait(false);
            }
            else
            {
                endpoint = await _busControl.GetSendEndpoint(
                    new Uri($"{_options.RabbitMQUri}/{command.GetType().Name}"))
                    .ConfigureAwait(false);
            }

            await endpoint.Send(command).ConfigureAwait(false);
        }

        public virtual async Task Send(ICommand command, Type commandType)
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before sending commands.");
            }
            
            ISendEndpoint endpoint;
            if (_options.UseInMemoryBus)
            {
                endpoint = await _busControl.GetSendEndpoint(
                    new Uri($"loopback://localhost/{commandType.Name}"))
                    .ConfigureAwait(false);
            }
            else
            {
                endpoint = await _busControl.GetSendEndpoint(
                    new Uri($"{_options.RabbitMQUri}/{commandType.Name}"))
                    .ConfigureAwait(false);
            }

            await endpoint.Send(command, commandType).ConfigureAwait(false);
        }
        
        public virtual Task Publish<T>(T @event) where T : class, IDomainEvent
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before publishing events.");
            }
            
            return _busControl.Publish(@event, @event.GetType());
        }

        public virtual Task Publish(IDomainEvent @event, Type eventType)
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before publishing events.");
            }
            
            return _busControl.Publish(@event, eventType);
        }

        public virtual void RegisterHandler<T>(Action<T> handler) where T : class, IMessage
        {
            var consumer = new HandlerConfigurator<T>(h =>
            {
                handler(h.Message);
                return Task.FromResult(false);
            });

            AddEndpointConsumer(typeof(T).Name, consumer);
        }

        public virtual void RegisterHandler<T, TResult>(Func<T, Task<TResult>> handler)
            where T : class, IMessage
            where TResult : class, IResponse
        {
            var consumer = new HandlerConfigurator<T>(async h =>
            {
                var response = await handler(h.Message);
                await h.RespondAsync(response);
            });

            AddEndpointConsumer(typeof(T).Name, consumer);
        }


        public virtual void RegisterHandler<T>(Action<MessageFault<T>> handler) where T : class, IMessage
        {
            var consumer = new HandlerConfigurator<Fault<T>>(h =>
            {
                var fault = new MessageFault<T>();
                fault.FaultId = h.Message.FaultId;
                fault.MessageId = h.MessageId;
                fault.Message = h.Message.Message;
                fault.Errors = new List<Exception>();
                foreach (var ex in h.Message.Exceptions)
                {
                    var netEx = new Exception(ex.Message)
                    {
                        Source = ex.Source,
                    };
                    netEx.Data.Add("MassTransitStackTrace", ex.StackTrace);
                    netEx.Data.Add("MassTransitExceptionType", ex.ExceptionType);

                    fault.Errors.Add(netEx);
                }

                handler(fault);

                return Task.FromResult(false);
            });
            
            AddEndpointConsumer(typeof(T).Name, consumer);
        }
        
        private void AddEndpointConsumer(string queueName, IReceiveEndpointSpecification spec)
        {
            if (_handlers.Keys.Contains(queueName))
            {
                _handlers[queueName].Add(spec);
            }
            else
            {
                var configs = new List<IReceiveEndpointSpecification>
                {
                    spec
                };
                
                _handlers.Add(queueName, configs);
            }
        }

        // Override and inject if you need a more custom startup configuration
        public virtual Task StartAsync()
        {
            var handlers = _manager.GetAllHandlers()
                .Where(h => h.ImplementationType.CanBeCastTo<IConsumer>());

            foreach(var handler in handlers)
            {
                ConsumerConfiguratorCache.Cache(handler.ImplementationType);
            }

            if (_options.UseInMemoryBus)
            {
                _busControl = Bus.Factory.CreateUsingInMemory(sbc =>
                {
                    if (_options.BusObserver != null)
                    {
                        sbc.AddBusFactorySpecification(_options.BusObserver);
                    }

                    sbc.UseRetry(Retry.Immediate(5));

                    foreach (var handler in handlers)
                    {
                        sbc.ReceiveEndpoint(handler.MessageType.Name, c =>
                        {
                            c.LoadFrom(_services);
                        });
                    }
                });
            }
            else
            {
                _busControl = Bus.Factory.CreateUsingRabbitMq(sbc =>
                {
                    var host = sbc.Host(new Uri(_options.RabbitMQUri), h =>
                    {
                        h.Username(_options.RabbitMQUsername);
                        h.Password(_options.RabbitMQPassword);
                    });

                    if (_options.BusObserver != null)
                    {
                        sbc.AddBusFactorySpecification(_options.BusObserver);
                    }

                    sbc.UseRetry(Retry.Immediate(5));

                    foreach (var handler in handlers)
                    {
                        sbc.ReceiveEndpoint(host, handler.MessageType.Name, c =>
                        {
                            c.LoadFrom(_services);

                            foreach(var faultHandler in _manager.FaultHandlers)
                            {
                                ConsumerConfiguratorCache2.Configure(faultHandler, c, _services);
                            }
                        });
                    }

                });
            }

            return _busControl.StartAsync();
        }

        public virtual Task StopAsync(CancellationToken token = default(CancellationToken))
        {
            return _busControl.StopAsync(token);
        }
    }

    public static class ConsumerConfiguratorCache2
    {
        static CachedConfigurator GetOrAdd(HandlerRegistration registration)
        {
            return Cached.Instance.GetOrAdd(registration.ImplementationType, _ =>
                (CachedConfigurator)Activator.CreateInstance(typeof(CachedConfigurator<>).MakeGenericType(registration.MessageType)));
        }

        public static void Configure(HandlerRegistration registration, IReceiveEndpointConfigurator configurator, IServiceProvider container)
        {
            GetOrAdd(registration).Configure(configurator, container);
        }

        public static void Cache(HandlerRegistration registration)
        {
            GetOrAdd(registration);
        }

        public static IEnumerable<Type> GetConsumers()
        {
            return Cached.Instance.Keys;
        }        

        interface CachedConfigurator
        {
            void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services);
        }

        class CachedConfigurator<T> : CachedConfigurator
            where T : class, IMessage
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new FaultConsumerFactory<T>(services));
            }
        }

        static class Cached
        {
            internal static readonly ConcurrentDictionary<Type, CachedConfigurator> Instance =
                new ConcurrentDictionary<Type, CachedConfigurator>();
        }
    }
}