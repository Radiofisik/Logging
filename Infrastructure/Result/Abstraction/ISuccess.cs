using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Result.Abstraction
{
    public interface ISuccess<out TSuccess>: IResult<TSuccess>
    {
        TSuccess Value { get; }
    }
}
