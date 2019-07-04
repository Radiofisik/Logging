using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Dtos;
using Microsoft.Extensions.Logging;
using Services.Abstractions;

namespace AutoFacLogging
{
    public class OnStart: IStartable
    {
        private readonly ILogger _logger;
        private readonly IExampleService _exampleService;

        public OnStart(ILogger logger, IExampleService exampleService)
        {
            _logger = logger;
            _exampleService = exampleService;
        }

        public void Start()
        {
            _logger.LogInformation("test");
            _exampleService.DoSomething(new InputDto() {SomeParam = "inputValue"});
        }
    }
}
