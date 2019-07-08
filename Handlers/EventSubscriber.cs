using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Events;
using Rebus.Bus;

namespace Handlers
{
    internal class EventSubscriber: IStartable
    {
        private readonly IBus _bus;

        public EventSubscriber(IBus bus)
        {
            _bus = bus;
        }

        public void Start()
        {
            _bus.Subscribe<TestEvent>().Wait();
        }
    }
}
