using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Abstractions;
using Infrastructure.Session.Abstraction;
using Rebus.Bus;

namespace Infrastructure.Rebus
{
    public class EventBus: IEventBus
    {
        private readonly IBus _bus;
        private readonly ISessionStorage _sessionStorage;

        public EventBus(IBus bus, ISessionStorage sessionStorage)
        {
            _bus = bus;
            _sessionStorage = sessionStorage;
        }

        public Task Publish<TEvent>(TEvent @event)
        {
            return _bus.Publish(@event, _sessionStorage.GetTraceHeaders());
        }
    }
}
