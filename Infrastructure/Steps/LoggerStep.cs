using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Infrastructure.Steps
{
    public class LoggerStep: IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionScope = context.Load<ITransactionContext>();
            var scope = transactionScope.GetOrNull<ILifetimeScope>("current-autofac-lifetime-scope");
            var logger = scope.Resolve<ILogger<LoggerStep>>();

            MessageContext.Current.Headers.TryGetValue("rbs2-sender-address", out string eventSender);
            MessageContext.Current.Headers.TryGetValue("rbs2-msg-type", out string eventType);
            using (logger.BeginScope(new Dictionary<string, object>(){{"exampleParam", "exampleParamValue"}}))
            {
                logger.LogInformation("Event type {EventType} from {EventSender} headers: {Headers}", eventType, eventSender, JsonConvert.SerializeObject(MessageContext.Current.Headers));
                logger.LogDebug("Event body: {Body}", JsonConvert.SerializeObject(MessageContext.Current.Message.Body));
                await next();
            }
        }
    }
}
