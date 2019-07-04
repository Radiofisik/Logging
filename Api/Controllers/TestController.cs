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
    [Route("api/test")]
    public class TestController : BaseController
    {
        private readonly IExampleService _exampleService;

        public TestController(IExampleService exampleService)
        {
            _exampleService = exampleService;
        }

        [HttpGet("do-something")]
        public async Task<IActionResult> DoSomething(InputDto dto)
        {
            var result = await _exampleService.DoSomething(dto);
            return Result(result);
        }
    }
}
