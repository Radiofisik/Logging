using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Interceptor
{
    public class LoggingInterceptor : IInterceptor
    {
        private readonly ILogger _logger;

        public LoggingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            using (_logger.BeginScope("{TargetType}.{Method}", invocation.TargetType.Name, invocation.Method.Name))
            {
                _logger.LogDebug("Arguments: [{Arguments}]", invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));

                try
                {
                    invocation.Proceed();
                }
                catch (Exception ex)
                {
                    _logger.LogError("{Exception} happened Arguments: [{Arguments}]", ex, invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
                }

                _logger.LogDebug("Result of {Method} invocation is {Result}", invocation.Method.Name, JsonConvert.SerializeObject(invocation.ReturnValue));
            }
        }
    }
}