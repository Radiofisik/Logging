using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Projections.Abstraction;
using Infrastructure.Result.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Api
{
    public class HttpResult<TResult>: IActionResult
    {
        private readonly IResult<TResult> _result;
        private readonly IEnumerable<IResultProjection<TResult>> _projections;

        public HttpResult(IResult<TResult> result, IEnumerable<IResultProjection<TResult>> projections)
        {
            _result = result;
            _projections = projections;
        }


        public async Task ExecuteResultAsync(ActionContext context)
        {
            var result = Project();
            if (result is ObjectResult objectResult)
            {
                await context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ObjectResult>>()
                    .ExecuteAsync(context, objectResult);
            } 

        }

        private IActionResult Project()
        {
            foreach (var projection in _projections)
            {
                if (projection.IsMatch(_result))
                {
                    return projection.Map(_result);
                }
            }
            throw new Exception($"projection was not found for type {_result.GetType().FullName}");
        }
    }
}
