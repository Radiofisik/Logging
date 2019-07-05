using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Result.Abstraction;

namespace Infrastructure.Result.Implementation
{
    public class Fail<TSuccess> : IFailure<TSuccess, ExceptionError>
    {
        public Fail(Exception ex)
        {
            Value = new ExceptionError(ex);
        }

        public ExceptionError Value { get; }
    }
}