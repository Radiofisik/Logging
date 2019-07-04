using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Result.Abstraction
{
    public interface IFailure<out TSuccess, out TError>: IResult<TSuccess>
        where TError : IError
    {
        TError Value { get; }
    }
}
