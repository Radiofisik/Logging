using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Projections.Abstraction;
using Infrastructure.Result.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Api.Projections.Implementation
{
    public class FailProjection<TResult>: IResultProjection<TResult>
    {
        public bool IsMatch(IResult<TResult> result) => result is IFailure<TResult, IError>;
        
        public IActionResult Map(IResult<TResult> result)
        {
            var fail = result as IFailure<TResult, IError>;
            var objResult = new ObjectResult(fail.Value)
            {
                StatusCode = 400
            };
            return objResult;
        }
    }
}
