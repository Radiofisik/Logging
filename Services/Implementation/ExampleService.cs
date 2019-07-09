using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dtos;
using Events;
using Infrastructure.Abstractions;
using Infrastructure.Result.Abstraction;
using Infrastructure.Result.Implementation;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Services.Abstractions;

namespace Services.Implementation
{
    internal sealed class ExampleService: IExampleService
    {
        private readonly ILogger<ExampleService> _logger;
        private readonly IEventBus _bus;

        public ExampleService(ILogger<ExampleService> logger, IEventBus bus)
        {
            _logger = logger;
            _bus = bus;
        }

        public async Task<IResult<OutputDto>> DoSomething(InputDto input)
        {
//            return new Fail<OutputDto>();

            _logger.LogInformation("log inside DoSomething");
            await _bus.Publish(new TestEvent(){Content = "event content"});

//           throw new Exception("something went wrong");
           var result = new OutputDto()
           {
               SomeParam = "outputValue"
           };
           return new Success<OutputDto>(result);
        }
    }
}
