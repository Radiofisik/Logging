using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Result.Abstraction;

namespace Infrastructure.Result.Implementation
{
    public class ExceptionError: IError
    {
        public ExceptionError(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
