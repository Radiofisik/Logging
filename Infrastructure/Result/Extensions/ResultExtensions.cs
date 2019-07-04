using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Result.Implementation;

namespace Infrastructure.Result.Extensions
{
    public static class ResultExtensions
    {
        public static Success<T> Success<T>(this T result)
        {
            return new Success<T>(result);
        }
    }
}
