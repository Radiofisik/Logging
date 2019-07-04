using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Infrastructure.Result.Implementation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Interceptor
{
    public class LoggingInterceptor : AsyncInterceptor
    {
        private readonly ILogger _logger;

        public LoggingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        protected override void InterceptSync(IInvocation invocation)
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

        protected override async Task InterceptAsync(IInvocation invocation, Type methodReturnType)
        {
            using (_logger.BeginScope("{TargetType}.{Method}", invocation.TargetType.Name, invocation.Method.Name))
            {
                try
                {
                    try
                    {
                        _logger.LogDebug("Arguments: [{Arguments}]", invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
                    }
                    catch (Exception)
                    {
                    }

                    invocation.Proceed();
                    Task result = (Task) invocation.ReturnValue;
                    await result;
                }
                catch (Exception e)
                {
                    Type[] typeParams = new Type[] {invocation.Method.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0]};
                    Type constructedType = typeof(Fail<>).MakeGenericType(typeParams);
                    var errorInstance = Activator.CreateInstance(constructedType, e);

                    var returnResult = Activator.CreateInstance(invocation.Method.ReturnType, BindingFlags.Instance
                                                                                              | BindingFlags.NonPublic
                                                                                              | BindingFlags.CreateInstance,
                        null, new object[] {errorInstance}, null, null);
                    invocation.ReturnValue = returnResult;
                    _logger.LogWarning("Error happened while executing of {TargetType}.{Method} exception is {Exception} with Arguments: [{Arguments}]",
                        invocation.TargetType.Name, invocation.Method.Name, JsonConvert.SerializeObject(e), invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
                }
            }
        }
    }
}