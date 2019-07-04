using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Result.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Projections.Abstraction
{
    public interface IResultProjection<in TResult>
    {
        bool IsMatch(IResult<TResult> result);

        IActionResult Map(IResult<TResult> result);
    }
}
