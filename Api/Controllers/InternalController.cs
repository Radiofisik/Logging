using Dtos;
using Infrastructure.Api;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Route("api/internal")]
    public class InternalController : BaseController
    {
        private readonly IExampleService _exampleService;

        public InternalController(IExampleService exampleService)
        {
            _exampleService = exampleService;
        }

        [HttpGet("do-something")]
        public async Task<IActionResult> DoSomethingInternal()
        {
            var result = await _exampleService.DoSomethingInternal();
            return Result(result);
        }
    }
}
