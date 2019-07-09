using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Abstractions
{
    public interface IEventBus
    {
        Task Publish<TEvent>(TEvent @event);
    }
}
