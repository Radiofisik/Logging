using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Session.Implementation
{
    class Headers: Dictionary<string, string>
    {
        public string RequestId
        {
            get
            {
                if (!this.ContainsKey(Const.RequestId))
                {
                    this[Const.RequestId] = MakeRequestId();
                } 
                return this[Const.RequestId];
            }
        }

        public string Email
        {
            get
            {
                if (!this.ContainsKey(Const.Email))
                {
                    return null;
                }
                return this[Const.Email];
            }
        }

        public string CorrelationContext
        {
            get
            {
                if (!this.ContainsKey(Const.CorrelationContext))
                {
                    this[Const.CorrelationContext] = MakeCorrelationContext();
                }
                return this[Const.CorrelationContext];
            }
        }

        private string MakeCorrelationContext()
        {
            return RequestId;
        }


        private string MakeRequestId()
        {
            return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
        }

        public static class Const
        {
            public const string RequestId = "x-request-id";

            public const string CorrelationContext = "x-correlation-context";

            public const string Email = "x-user-email";
        }
    }
}
