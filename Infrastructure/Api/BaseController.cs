using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure.Api.Projections.Implementation;
using Infrastructure.Projections;
using Infrastructure.Projections.Abstraction;
using Infrastructure.Result.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Api
{
    public class BaseController: ControllerBase
    {
        protected virtual IEnumerable<IResultProjection<T>> GetProjections<T>() => new IResultProjection<T>[]
        {
            new SuccessProjection<T>(),
            new FailProjection<T>()
        };

        [NonAction]
        protected virtual IActionResult Result<T>(IResult<T> result)
        {
            return new HttpResult<T>(result, GetProjections<T>());
        }
    }
}
