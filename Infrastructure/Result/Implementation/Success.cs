using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Result.Abstraction;

namespace Infrastructure.Result.Implementation
{
    public class Success<T>: ISuccess<T>
    {
        public Success(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
