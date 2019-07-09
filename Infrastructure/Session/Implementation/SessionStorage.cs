using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Session.Abstraction;

namespace Infrastructure.Session.Implementation
{
    public class SessionStorage: ISessionStorage
    {
        private readonly Headers _headers;
        public SessionStorage()
        {
            _headers = new Headers();
        }

        public void SetHeaders(params (string Key, IEnumerable<string> Value)[] headers)
        {
            foreach (var (Key, Value) in headers)
            {
                _headers[Key.ToLower()] = String.Join("; ", Value);
            }
        }    

        public Dictionary<string, string> GetLoggingHeaders()
        {
            return new Dictionary<string, string>
            {
                { "RequestId", _headers.RequestId },
                { "CorrelationContext", _headers.CorrelationContext},
                { "Email", _headers.Email ?? "unknown"},
            };
        }

        public Dictionary<string, string> GetTraceHeaders()
        {
            return new Dictionary<string, string>
            {
                { Headers.Const.RequestId, _headers.RequestId },
                { Headers.Const.CorrelationContext, _headers.CorrelationContext},
                { Headers.Const.Email, _headers.Email ?? "unknown"},
            };
        }
    }
}
