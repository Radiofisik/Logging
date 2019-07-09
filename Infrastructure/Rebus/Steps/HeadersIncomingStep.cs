using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Infrastructure.Session.Abstraction;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Infrastructure.Steps
{
    public class HeadersIncomingStep: IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionScope = context.Load<ITransactionContext>();
            var scope = transactionScope.GetOrNull<ILifetimeScope>("current-autofac-lifetime-scope");
            var message = context.Load<Message>();
            var sessionStorage = scope.Resolve<ISessionStorage>();

            var headers = MessageContext.Current.Headers.Select(x => (x.Key, (new[] { x.Value }).AsEnumerable())).ToArray();
            sessionStorage.SetHeaders(headers);

            await next();
        }
    }
}
