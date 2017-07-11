using System;
using System.Threading.Tasks;
using MassTransit;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using System.Linq;
using System.Threading;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitMessageBus : IEventPublisher, ICommandSender, ICommandSenderReceiver
    {
        private readonly ILogger<MassTransitMessageBus> _logger;
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

        public virtual async Task<TResult> Send<TResult>(ICommand command) where TResult : class, IResponse
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before sending commands.");
            }

            var type = command.GetType(); 

            Uri endpoint;
            if (_options.UseInMemoryBus)
            {
                endpoint = new Uri($"loopback://localhost/{type.Name}");
            }
            else
            {
                endpoint = new Uri($"{_options.RabbitMQUri}/{type.Name}");
            }

            var createObject = typeof(MessageRequestClient<,>);
            var createGeneric = createObject.MakeGenericType(new Type[] { type, typeof(TResult) });
            var createInstance = Activator.CreateInstance(createGeneric, new object[] { _busControl, endpoint, TimeSpan.FromSeconds(30), default(TimeSpan?), null });
            var requestMethod = createInstance.GetType().GetMethod("Request");
            var response = (dynamic)requestMethod.Invoke(createInstance, new object[] { command, new CancellationToken() });
            return await response;
        }

        public virtual async Task<TResult> Send<TResult>(ICommand command, Type commandType) where TResult : class, IResponse
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before sending commands.");
            }

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

        public virtual async Task Send(ICommand command)
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before sending commands.");
            }

            var commandType = command.GetType();

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
        
        public virtual Task Publish(IDomainEvent @event)
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

        // Override and inject if you need a more custom startup configuration
        public virtual Task StartAsync(Type[] listenerNoQueueTypes = null)
        {
            if (_options.UseInMemoryBus)
            {
                throw new NotSupportedException("InMemory bus is not currently supported");
                // _busControl = Bus.Factory.CreateUsingInMemory(sbc =>
                // {
                //     if (_options.BusObserver != null)
                //     {
                //         sbc.AddBusFactorySpecification(_options.BusObserver);
                //     }

                //     sbc.UseRetry(Retry.Immediate(5));

                //     foreach (var handler in handlers)
                //     {
                //         sbc.ReceiveEndpoint(handler.MessageType.Name, c =>
                //         {
                //             c.LoadFrom(_services);
                //         });
                //     }
                // });
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

                    sbc.ConfigureJsonDeserializer(c => {
                        c.NullValueHandling = NullValueHandling.Include;
                        c.DefaultValueHandling = DefaultValueHandling.Include;
                        return c;
                    });

                    sbc.ConfigureJsonSerializer(c => {
                        c.NullValueHandling = NullValueHandling.Include;
                        c.DefaultValueHandling = DefaultValueHandling.Include;
                        return c;
                    });

                    if (_options.BusObserver != null)
                    {
                        sbc.AddBusFactorySpecification(_options.BusObserver);
                    }

                    if (_options.RetryPolicy != null)
                    {
                        sbc.UseRetry(_options.RetryPolicy);
                    }

                    var allHandlers = _manager.GetAllHandlers().ToList();

                    foreach (var msgTypes in allHandlers
                        .Where(h => h.GenericType == typeof(IDistributedEventListener<>))
                        .GroupBy(h => h.ImplementationType))
                    {
                        sbc.ReceiveEndpoint(host, c =>
                        {
                            foreach (var handler in msgTypes)
                            {
                                ConsumerConfiguratorCache.Configure(handler, c, _services);
                            }
                        });
                    }

                    foreach (var msgTypes in allHandlers
                        .Where(h => h.GenericType == typeof(IEventListener<>) || h.GenericType == typeof(IFaultHandler<>))
                        .GroupBy(h => h.ImplementationType))
                    {
                        sbc.ReceiveEndpoint(host, msgTypes.Key.Name, c =>
                        {
                            foreach (var handler in msgTypes)
                            {
                                ConsumerConfiguratorCache.Configure(handler, c, _services);
                            }
                        });
                    }

                    foreach (var msgTypes in allHandlers
                        .Where(h => h.GenericType == typeof(ICommandHandler<,>) || h.GenericType == typeof(ICommandHandler<>))
                        .GroupBy(h => h.MessageType))
                    {
                        sbc.ReceiveEndpoint(host, msgTypes.Key.Name, c =>
                        {
                            foreach (var handler in msgTypes)
                            {
                                ConsumerConfiguratorCache.Configure(handler, c, _services);
                            }
                        });
                    }
                });
            }

            if (_options.ReceiveObserver != null)
            {
                _busControl.ConnectReceiveObserver(_options.ReceiveObserver);
            }

            if (_options.SendObserver != null)
            {
                _busControl.ConnectSendObserver(_options.SendObserver);
            }

            if (_options.ConsumeObserver != null)
            {
                _busControl.ConnectConsumeObserver(_options.ConsumeObserver);
            }

            if (_options.PublishObserver != null)
            {
                _busControl.ConnectPublishObserver(_options.PublishObserver);
            }

            return _busControl.StartAsync();
        }

        public virtual Task StopAsync(CancellationToken token = default(CancellationToken))
        {
            return _busControl.StopAsync(token);
        }
    }
}