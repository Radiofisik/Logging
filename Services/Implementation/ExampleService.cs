using System;
using System.Collections.Generic;
using System.Text;
using Dtos;
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

        public OutputDto DoSomething(InputDto input)
        {
            _logger.LogInformation("log inside DoSomething");
           return new OutputDto()
           {
               SomeParam = "outputValue"
           };
        }
    }
}
