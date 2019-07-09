using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Session.Abstraction;
using Infrastructure.Session.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.MiddleWare
{
    public class SessionMiddleWare
    {
        private readonly RequestDelegate _next;

        public SessionMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ISessionStorage sessionStorage, ILogger logger)
        {
            if (!context.Request.Headers.ContainsKey(Headers.Const.RequestId))
            {
                context.Request.Headers.Add(Headers.Const.RequestId, context.TraceIdentifier);
            }

            var headers = context.Request.Headers.Select(x => (x.Key, x.Value.AsEnumerable())).ToArray();
            sessionStorage.SetHeaders(headers);

            await _next(context);
        }
    }
}
