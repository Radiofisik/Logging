using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dtos;
using Infrastructure.Result.Abstraction;
using Infrastructure.Result.Implementation;
using Microsoft.Extensions.Logging;
using Services.Abstractions;

namespace Services.Implementation
{
    internal sealed class ExampleService: IExampleService
    {
        private readonly ILogger<ExampleService> _logger;

        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }

        public async Task<IResult<OutputDto>> DoSomething(InputDto input)
        {
            _logger.LogInformation("log inside DoSomething");
//           throw new Exception("something went wrong");
           var result = new OutputDto()
           {
               SomeParam = "outputValue"
           };
           return new Success<OutputDto>(result);
        }
    }
}
