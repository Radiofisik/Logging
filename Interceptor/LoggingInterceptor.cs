using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Interceptor
{
    public class LoggingInterceptor: IInterceptor
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
                try
                {
                    _logger.LogDebug("Arguments: [{Arguments}]", invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
                }
                catch (Exception)
                {
                }

                invocation.Proceed();
            }
        }
    }
}
