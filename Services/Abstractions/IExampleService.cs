using Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Result.Abstraction;

namespace Services.Abstractions
{
    public interface IExampleService: IService
    {
        Task<IResult<OutputDto>> DoSomething(InputDto input);
    }
}
