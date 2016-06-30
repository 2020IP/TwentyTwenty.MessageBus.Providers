using System;
using System.Threading.Tasks;
using MassTransit;
using TwentyTwenty.DomainDriven;
using TwentyTwenty.DomainDriven.CQRS;
using System.Collections.Generic;
using MassTransit.ConsumeConfigurators;
using System.Linq;

namespace TwentyTwenty.MessageBus.Providers.MassTransit
{
    public class MassTransitMessageBus : IEventPublisher, ICommandSender, IHandlerRegistrar, IFaultHandlerRegistrar
    {
        private readonly Dictionary<string, List<IReceiveEndpointSpecification>> _handlers = 
            new Dictionary<string, List<IReceiveEndpointSpecification>>();
        private readonly MassTransitMessageBusOptions _options;
        private IBusControl _busControl = null;

        public MassTransitMessageBus(MassTransitMessageBusOptions options)
        {
            _options = options;
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
                    new Uri("loopback://localhost/" + command.GetType().Name))
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
        
        public virtual Task Publish<T>(T @event) where T : class, IDomainEvent
        {
            if (_busControl == null)
            {
                throw new InvalidOperationException("MassTransit bus must be started before publishing events.");
            }
            
            return _busControl.Publish(@event, @event.GetType());
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
        
        public virtual void RegisterHandler<T>(Action<MessageFault<T>> handler) where T : class, IMessage
        {
            var consumer = new HandlerConfigurator<Fault<T>>(h =>
            {
                var fault = new MessageFault<T>();
                fault.FaultId = h.Message.FaultId;
                fault.Message = h.Message.Message;
                fault.ErrorMessage = string.Join(", ", h.Message.Exceptions.Select(e => e.Message));
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
            if (_options.UseInMemoryBus)
            {
                _busControl = Bus.Factory.CreateUsingInMemory(sbc =>
                {
                    if (_options.BusObserver != null)
                    {
                        sbc.AddBusFactorySpecification(_options.BusObserver);
                    }

                    sbc.UseRetry(Retry.Immediate(5));

                    foreach (var kvp in _handlers)
                    {
                        sbc.ReceiveEndpoint(kvp.Key, ep =>
                        {
                            foreach (var spec in kvp.Value)
                            {
                                ep.AddEndpointSpecification(spec);
                            }
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

                    foreach (var kvp in _handlers)
                    {
                        sbc.ReceiveEndpoint(host, kvp.Key, ep =>
                        {
                            foreach (var spec in kvp.Value)
                            {
                                ep.AddEndpointSpecification(spec);
                            }
                        });
                    }
                });
            }

            return _busControl.StartAsync();
        }
    }
}