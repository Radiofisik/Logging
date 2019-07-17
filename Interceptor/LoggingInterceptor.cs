using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Castle.DynamicProxy;
using Infrastructure.Result.Abstraction;
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
                LogArguments(invocation);
                try
                {
                    invocation.Proceed();
                }
                catch (Exception e)
                {
                    LogException(invocation, e);
                    throw;
                }
            }
        }


        protected override async Task InterceptAsync(IInvocation invocation, Type methodReturnType)
        {
            using (_logger.BeginScope("{TargetType}.{Method}", invocation.TargetType.Name, invocation.Method.Name))
            {
                try
                {
                    LogArguments(invocation);
                    invocation.Proceed();
                    Task result = (Task) invocation.ReturnValue;
                    await result;

                    var resultProperty = result.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        var resultValue = resultProperty.GetValue(result);
                        var resultInterfaces = resultValue.GetType().GetInterfaces();

                        if (resultInterfaces.Any(resultInterface => resultInterface.IsGenericType
                                                                       && resultInterface.GetGenericTypeDefinition() ==
                                                                       typeof(ISuccess<>)))
                        {
                            LogResult(resultValue);
                        }
                        else
                        {
                            LogErrorResult(resultValue);
                        }
                    }
                }
                catch (Exception e)
                {
                    Type returnType = invocation.Method.ReturnType;

                    if (returnType.IsGenericType
                        && returnType.GetGenericTypeDefinition() == typeof(Task<>)
                        && returnType.GenericTypeArguments.Length == 1
                        && returnType.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(IResult<>)
                        && returnType.GenericTypeArguments[0].GenericTypeArguments.Length == 1
                    )
                    {
                        Type[] tSuccess = new Type[] {returnType.GenericTypeArguments[0].GenericTypeArguments[0]};
                        invocation.ReturnValue = MakeTaskOfResultOfFail(returnType, tSuccess, e);
                    }

                    LogException(invocation, e);
                }
            }
        }


        private object MakeTaskOfResultOfFail(Type returnType, Type[] tSuccess, Exception e)
        {
            Type constructedType = typeof(Fail<>).MakeGenericType(tSuccess);
            var errorInstance = Activator.CreateInstance(constructedType, e);

            var returnResult = Activator.CreateInstance(returnType, BindingFlags.Instance
                                                                    | BindingFlags.NonPublic
                                                                    | BindingFlags.CreateInstance,
                null, new object[] {errorInstance}, null, null);
            return returnResult;
        }

        private void LogException(IInvocation invocation, Exception e)
        {
            _logger.LogWarning(
                "Error happened while executing of {TargetType}.{Method} exception is {Exception} with Arguments: [{Arguments}]",
                invocation.TargetType.Name, invocation.Method.Name, JsonConvert.SerializeObject(e),
                invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
        }

        private void LogResult(object result)
        {
            try
            {
                _logger.LogInformation("Result of method invokation is {Result}", JsonConvert.SerializeObject(result));
            }
            catch (Exception)
            {
            }
        }

        private void LogErrorResult(object resultValue)
        {
            try
            {
                _logger.LogError("Result of method invokation is ERROR {Result}",
                    JsonConvert.SerializeObject(resultValue));
            }
            catch (Exception)
            {
            }
        }

        private void LogArguments(IInvocation invocation)
        {
            try
            {
                _logger.LogDebug("Arguments: [{Arguments}]",
                    invocation.Arguments.Select(x => JsonConvert.SerializeObject(x)));
            }
            catch (Exception)
            {
            }
        }
    }
}